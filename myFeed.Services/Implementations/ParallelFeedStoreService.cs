﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using myFeed.Repositories.Abstractions;
using myFeed.Repositories.Models;
using myFeed.Services.Abstractions;

namespace myFeed.Services.Implementations
{
    public sealed class ParallelFeedStoreService : IFeedStoreService
    {
        private readonly ICategoriesRepository _categoriesRepository;
        private readonly IFeedFetchService _feedFetchService;
        
        public ParallelFeedStoreService(
            ICategoriesRepository categoriesRepository,
            IFeedFetchService feedFetchService)
        {
            _categoriesRepository = categoriesRepository;
            _feedFetchService = feedFetchService;
        }
        
        public Task<(IEnumerable<Exception>, IOrderedEnumerable<Article>)> LoadAsync(
            IEnumerable<Channel> channels) => Task.Run(async () =>
        {
            const int maxArticleCountAllowed = 70;
            var fetchables = channels.Select(i => 
            {
                // Remove extra saved articles and save changes.
                if (i.Articles.Count <= maxArticleCountAllowed) return i;
                i.Articles = i.Articles
                    .OrderByDescending(x => x.PublishedDate)
                    .Take(maxArticleCountAllowed).ToList();
                _categoriesRepository.UpdateChannelAsync(i);
                return i;
            });

            // Read items into lookup using title and date as keys.
            var existingLookup = fetchables
                .SelectMany(i => i.Articles)
                .ToLookup(i => (i.Title, i.FeedTitle));

            // Retrieve feed based on single fetcher implementation.
            var grouppedArticles = await Task.WhenAll(fetchables.Select(FetchAsync));
            var distinctGroupping = grouppedArticles
                .Select(i => (i.Item1, i.Item3
                    .Where(x => !existingLookup.Contains((x.Title, x.FeedTitle)))
                    .ToArray()))
                .ToList();
                
            // Save received distinct items into database.
            foreach (var grouping in distinctGroupping) 
                await _categoriesRepository.InsertArticleRangeAsync(
                    grouping.Item1, grouping.Item2);

            // Return global join with both old and new articles.
            var flatternedArticles = distinctGroupping.SelectMany(i => i.Item2);
            var errors = grouppedArticles.Select(i => i.Item2);
            return (errors, existingLookup
                .SelectMany(i => i)
                .Concat(flatternedArticles)
                .OrderByDescending(i => i.PublishedDate));
        });

        private Task<(Channel, Exception, IEnumerable<Article>)> FetchAsync(
            Channel fetchableChannel) => Task.Run(async () =>
        {
            // Fetches single feed in a separate thread.
            var uriToFetch = fetchableChannel.Uri;
            (var exception, var articles) = await _feedFetchService.FetchAsync(uriToFetch);
            return (fetchableChannel, exception, articles
                .OrderByDescending(i => i.PublishedDate)
                .Take(70));
        });
    }
}