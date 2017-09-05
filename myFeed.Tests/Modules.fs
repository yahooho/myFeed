namespace myFeed.Tests.Modules

open Xunit
open Autofac
open Moq

open System

open myFeed.Repositories
open myFeed.Repositories.Entities
open myFeed.Repositories.Abstractions

open myFeed.Tests.Extensions
open myFeed.Tests.Extensions.Mocking
open myFeed.Tests.Extensions.DependencyInjection

open myFeed.Services
open myFeed.Services.Abstractions
open myFeed.Services.Implementations

open myFeed.ViewModels
open myFeed.ViewModels.Implementations

// Tests for modules registrations.
module RegistrationsTestsModule =    

    [<Fact>]
    let ``all data providers should be registered``() =
        ContainerBuilder() 
        |> tee registerModule<RepositoriesModule>
        |> buildScope
        |> tee assertResolve<EntityContext> 
        |> tee assertResolve<IArticlesRepository>
        |> tee assertResolve<ISourcesRepository>
        |> tee assertResolve<IConfigurationRepository>
        |> dispose

    [<Fact>]
    let ``all default services should be registered``() = 
        ContainerBuilder()
        |> tee registerModule<ServicesModule> 
        |> tee registerMockInstance<IPlatformProvider>
        |> tee registerMockInstance<ITranslationsService>
        |> buildScope
        |> tee assertResolve<EntityContext> 
        |> tee assertResolve<ISourcesRepository>
        |> tee assertResolve<IConfigurationRepository>
        |> tee assertResolve<IArticlesRepository>
        |> tee assertResolve<ISearchService>
        |> tee assertResolve<IOpmlService>
        |> tee assertResolve<ISerializationService>
        |> tee assertResolve<IHtmlParsingService>
        |> tee assertResolve<IFeedService>
        |> dispose

    let ``all default viewmodels should be registered``() =
        ContainerBuilder()
        |> tee registerModule<ViewModelsModule>
        |> tee registerMockInstance<IPlatformProvider>
        |> tee registerMockInstance<ITranslationsService>
        |> buildScope
        |> tee assertResolve<FaveViewModel>
        |> tee assertResolve<FeedViewModel>
        |> tee assertResolve<SearchViewModel>
        |> tee assertResolve<SettingsViewModel>
        |> tee assertResolve<SourcesViewModel>
        |> dispose    