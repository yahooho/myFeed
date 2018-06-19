using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using DryIocAttributes;
using myFeed.Interfaces;
using myFeed.Models;
using myFeed.Platform;
using PropertyChanged;
using ReactiveUI;

namespace myFeed.ViewModels
{
    [Reuse(ReuseType.Transient)]
    [AddINotifyPropertyChangedInterface]
    [ExportEx(typeof(FeedGroupViewModel))]
    public sealed class FeedGroupViewModel
    {
        private readonly Func<Article, FeedItemViewModel> _factory;
        private readonly IReactiveList<FeedItemViewModel> _source;
        private readonly INavigationService _navigationService;
        private readonly IFeedStoreService _feedStoreService;
        private readonly ISettingManager _settingManager;
        private readonly Category _category;
        
        public ReactiveCommand<Unit, IEnumerable<Article>> Fetch { get; }
        public IReactiveDerivedList<FeedItemViewModel> Items { get; }
        public Interaction<Exception, bool> Error { get; }
        public ReactiveCommand<Unit, Unit> Modify { get; }

        public bool IsLoading { get; private set; } = true;
        public bool ShowRead { get; private set; } = true;
        public bool IsEmpty { get; private set; }
        public string Title => _category.Title;

        public FeedGroupViewModel(
            Func<Article, FeedItemViewModel> factory,
            INavigationService navigationService,
            IFeedStoreService feedStoreService,
            ISettingManager settingManager,
            Category category)
        {
            _navigationService = navigationService;
            _feedStoreService = feedStoreService;
            _settingManager = settingManager;
            _category = category;
            _factory = factory;

            _source = new ReactiveList<FeedItemViewModel> {ChangeTrackingEnabled = true};
            Items = _source.CreateDerivedCollection(x => x, x => !(!ShowRead && x.Read));
            Fetch = ReactiveCommand.CreateFromTask(
                () => _feedStoreService.Load(_category.Channels)
            );
            
            Observable.Return(Unit.Default)
                .SelectMany(_ => _settingManager.Read())
                .Select(settings => settings.Read)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(x => ShowRead = x);
            Fetch.ObserveOn(RxApp.MainThreadScheduler)
                .Do(articles => _source.Clear())
                .SelectMany(articles => articles)
                .Select(_factory)
                .Subscribe(_source.Add);

            Fetch.IsExecuting.Skip(1)
                .Subscribe(x => IsLoading = x);
            Items.CountChanged
                .Select(count => count == 0)
                .Subscribe(x => IsEmpty = x);

            Error = new Interaction<Exception, bool>();
            Modify = ReactiveCommand.CreateFromTask(
                () => _navigationService.Navigate<ChannelViewModel>()
            );
            Fetch.ThrownExceptions
                .ObserveOn(RxApp.MainThreadScheduler)
                .SelectMany(error => Error.Handle(error))
                .Where(retryRequested => retryRequested)
                .Select(x => Unit.Default)
                .InvokeCommand(Fetch);
        }
    }
}