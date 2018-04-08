﻿using System;
using System.Reactive;
using System.Reactive.Linq;
using DryIoc;
using DryIocAttributes;
using myFeed.Interfaces;
using myFeed.Models;
using myFeed.Platform;
using PropertyChanged;
using ReactiveUI;

namespace myFeed.ViewModels
{
    [Reuse(ReuseType.Transient)]
    [ExportEx(typeof(FeedItemViewModel))]
    [AddINotifyPropertyChangedInterface]
    public sealed class FeedItemViewModel
    {
        [DoNotCheckEquality]
        private Article Article { get; set; }
        
        public Interaction<Unit, bool> CopyConfirm { get; }
        public ReactiveCommand MarkFave { get; }
        public ReactiveCommand MarkRead { get; }
        public ReactiveCommand Launch { get; }
        public ReactiveCommand Share { get; }
        public ReactiveCommand Copy { get; }
        public ReactiveCommand Open { get; }

        public DateTime Published => Article.PublishedDate;
        public string Content => Article.Content;
        public string Image => Article.ImageUri;
        public string Title => Article.Title;
        public string Feed => Article.FeedTitle;
        public bool Fave => Article.Fave;
        public bool Read => Article.Read;

        public FeedItemViewModel(
            INavigationService navigationService,
            ICategoryManager categoryManager,
            IFavoriteManager favoriteManager,
            IPlatformService platformService,
            IResolver resolver, 
            Article article)
        {
            Article = article;
            CopyConfirm = new Interaction<Unit, bool>();
            Copy = ReactiveCommand.CreateFromTask(async () =>
            {
                await platformService.CopyTextToClipboard(Article.Uri);
                await CopyConfirm.Handle(Unit.Default);
            });
            Launch = ReactiveCommand.CreateFromTask(
                () => platformService.LaunchUri(new Uri(Article.Uri)),
                this.WhenAnyValue(x => x.Article).Select(x => Uri
                    .IsWellFormedUriString(x.Uri, UriKind.Absolute))
            );
            Share = ReactiveCommand.CreateFromTask(
                () => platformService.Share($"{Article.Title} {Article.Uri}")
            );
            Open = ReactiveCommand.CreateFromTask(async () =>
            {
                Article.Read = true;
                await categoryManager.UpdateArticleAsync(Article);
                var factory = resolver.Resolve<Func<FeedItemViewModel, ArticleViewModel>>();
                await navigationService.Navigate<ArticleViewModel>(factory(this));
                Article = Article;
            });
            MarkRead = ReactiveCommand.CreateFromTask(async () =>
            {
                Article.Read = !Article.Read;
                await categoryManager.UpdateArticleAsync(Article = Article);
            });
            MarkFave = ReactiveCommand.CreateFromTask(async () =>
            {
                if (!Fave) await favoriteManager.InsertAsync(Article = Article);
                else await favoriteManager.RemoveAsync(Article = Article);
            });
        }
    }
}