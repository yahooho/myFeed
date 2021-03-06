﻿using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using myFeed.Interfaces;
using myFeed.Models;
using myFeed.Services;
using myFeed.Tests.Attributes;
using Xunit;

namespace myFeed.Tests.Services
{
    public sealed class LiteSettingManagerTests
    {
        private readonly ISettingManager _settingManager;

        public LiteSettingManagerTests() => _settingManager = new LiteSettingManager(Environment.Database);
        
        [Theory]
        [InlineData(10.0)]
        [InlineData(20.21)]
        [InlineData(30.320)]
        [CleanUpCollection(nameof(Settings))]
        public async Task ShouldReadAndWriteSettings(double font)
        {
            var settings = new Settings {Font = font};
            var success = await _settingManager.Write(settings);
            var read = await _settingManager.Read();
            read.Font.Should().Be(font);
            success.Should().BeTrue();
        }
        
        [Fact]
        [CleanUpCollection(nameof(Settings))]
        public async Task ShouldSaveSettingsToNonVolatileStorage()
        {
            var settings = new Settings();
            foreach (var _ in Enumerable.Range(0, 200))
            {
                var success = await _settingManager.Write(settings);
                success.Should().BeTrue();
            }
            var read = Environment.Database.GetCollection<Settings>();
            read.FindAll().Count().Should().Be(1);
        }
        
        [Fact]
        [CleanUpCollection(nameof(Settings))]
        public async Task ShouldResolveDefaultValues()
        {
            var settings = await _settingManager.Read();
            settings.Period.Should().BeGreaterThan(0);
            settings.Font.Should().BeGreaterThan(0);
            settings.Max.Should().BeGreaterThan(0);
            settings.Banners.Should().Be(false);
            settings.Images.Should().Be(true);
        }
    }
}