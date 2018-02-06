﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using DryIocAttributes;
using myFeed.Platform;
using myFeed.Uwp.Controls;
using myFeed.Uwp.Views;
using myFeed.ViewModels;
using Microsoft.Toolkit.Uwp.UI.Extensions;
using DryIoc;

namespace myFeed.Uwp.Services
{
    [Reuse(ReuseType.Singleton)]
    [Export(typeof(INavigationService))]
    public sealed class UwpNavigationService : INavigationService
    {
        private readonly Subject<Type> _navigatedSubject = new Subject<Type>();
        private readonly IReadOnlyDictionary<Type, Type> _pages = new Dictionary<Type, Type>
        {
            {typeof(SettingViewModel), typeof(SettingView)},
            {typeof(ChannelViewModel), typeof(ChannelView)},
            {typeof(ArticleViewModel), typeof(ArticleView)},
            {typeof(SearchViewModel), typeof(SearchView)},
            {typeof(MenuViewModel), typeof(MenuView)},
            {typeof(FaveViewModel), typeof(FaveView)},
            {typeof(FeedViewModel), typeof(FeedView)}
        };
        public IReadOnlyDictionary<Type, object> Icons => new Dictionary<Type, object>
        {
            {typeof(FaveViewModel), Symbol.OutlineStar},
            {typeof(FeedViewModel), Symbol.PostUpdate},
            {typeof(SettingViewModel), Symbol.Setting},
            {typeof(ChannelViewModel), Symbol.List},
            {typeof(SearchViewModel), Symbol.Zoom}
        };
        public UwpNavigationService()
        {
            var systemNavigationManager = SystemNavigationManager.GetForCurrentView();
            systemNavigationManager.AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;
            systemNavigationManager.BackRequested += NavigateBack;

            var page = (Page)((Frame)Window.Current.Content).Content;
            var color = (Color)Application.Current.Resources["SystemChromeLowColor"];
            StatusBar.SetBackgroundColor(page, color);
            StatusBar.SetBackgroundOpacity(page, 1);
        }

        public IObservable<Type> Navigated => _navigatedSubject;

        public Task Navigate<T>() where T : class => Navigate<T>(App.Container.Resolve<T>());

        public async Task Navigate<T>(object parameter) where T : class
        {
            switch (typeof(T).Name)
            {
                case nameof(FeedViewModel):
                case nameof(FaveViewModel):
                case nameof(SearchViewModel):
                case nameof(ChannelViewModel):
                case nameof(SettingViewModel):
                    NavigateFrame(GetChild<Frame>(Window.Current.Content, 0));
                    break;
                case nameof(MenuViewModel):
                    NavigateFrame((Frame)Window.Current.Content);
                    break;
                case nameof(ArticleViewModel):
                    if (GetChild<Frame>(Window.Current.Content, 1) == null)
                        await Navigate<FeedViewModel>();
                    await Task.Delay(150);
                    NavigateFrame(GetChild<Frame>(Window.Current.Content, 1));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            void NavigateFrame(Frame frame)
            {
                var viewModelType = typeof(T);
                if ((Page)frame.Content != null &&
                   ((Page)frame.Content).DataContext.GetType() == viewModelType &&
                   ((Page)frame.Content).DataContext.GetType() != typeof(ArticleViewModel)) return;
                frame.Navigate(_pages[viewModelType], parameter);
                ((Page)frame.Content).DataContext = parameter;
                RaiseNavigated(viewModelType);
            }
        }

        private void NavigateBack(object sender, BackRequestedEventArgs e)
        {
            var splitView = GetChild<SwipeableSplitView>(Window.Current.Content, 0);
            if (splitView.IsSwipeablePaneOpen && splitView.DisplayMode == SplitViewDisplayMode.Overlay)
            {
                splitView.IsSwipeablePaneOpen = false;
                e.Handled = true;
                return;
            }

            var articleFrame = GetChild<Frame>(Window.Current.Content, 1);
            var splitViewFrame = GetChild<Frame>(Window.Current.Content, 0);
            var frame = articleFrame?.CanGoBack == true ? articleFrame
                      : splitViewFrame?.CanGoBack == true ? splitViewFrame : null;
            if (frame == null) return;

            var instance = frame.BackStack.Last().Parameter;
            frame.GoBack();
            e.Handled = true;
            frame.ForwardStack.Clear();
            if (instance == null) return;

            ((Page) frame.Content).DataContext = instance is ArticleViewModel
                ? instance : App.Container.Resolve(instance.GetType());
            RaiseNavigated(instance.GetType());
        }

        private void RaiseNavigated(Type type)
        {
            if (type == typeof(ArticleViewModel))
                type = ((Page)GetChild<Frame>(Window.Current.Content, 0)
                    .Content).DataContext.GetType();
            _navigatedSubject.OnNext(type);
        }

        private static T GetChild<T>(DependencyObject root, int depth) where T : DependencyObject
        {
            var childrenCount = VisualTreeHelper.GetChildrenCount(root);
            for (var x = 0; x < childrenCount; x++)
            {
                var child = VisualTreeHelper.GetChild(root, x);
                if (child is T ths && depth-- == 0) return ths;
                var frame = GetChild<T>(child, depth);
                if (frame != null) return frame;
            }
            return null;
        }
    }
}