﻿using Caliburn.Micro;
using Frontend.Core.Helpers;
using Frontend.Core.Logging;
using Frontend.Core.Model;
using Frontend.Core.Model.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Frontend.Core.Factories
{
    public class ConverterSettingsFactory : FactoryBase
    {
        /// <summary>
        /// The Game configuration factory
        /// </summary>
        private GameConfigurationFactory gameConfigurationFactory;

        /// <summary>
        /// The preference category factory
        /// </summary>
        private PreferenceCategoryFactory preferenceCategoryFactory;
        
        /// <summary>
        /// The list of game configurations
        /// </summary>
        private ObservableCollection<IGameConfiguration> gameConfigurations;

        /// <summary>
        /// The relative path to the game configuration config file
        /// </summary>
        private string relativeGameConfigurationPath;

        /// <summary>
        /// Initializes a new instance of the ConverterSettingsFactory
        /// </summary>
        /// <param name="eventAggregator">The event aggregator</param>
        public ConverterSettingsFactory(IEventAggregator eventAggregator)
            : base(eventAggregator, "converter")
        {
        }

        /// <summary>
        /// The game configuration factory self-resolving property
        /// </summary>
        protected GameConfigurationFactory GameConfigurationFactory
        {
            get
            {
                return this.gameConfigurationFactory ?? (this.gameConfigurationFactory = new GameConfigurationFactory(this.EventAggregator));
            }
        }

        /// <summary>
        /// The game configuration factory self-resolving property
        /// </summary>
        protected PreferenceCategoryFactory PreferenceCategoryFactory
        {
            get
            {
                return this.preferenceCategoryFactory ?? (this.preferenceCategoryFactory = new PreferenceCategoryFactory(this.EventAggregator));
            }
        }

        /// <summary>
        /// Helper property for turning the relative game configuration path into an absolute path.
        /// 
        /// What was I thinking?
        /// </summary>
        protected string AbsoluteGameConfigurationPath
        {
            get
            {
                return Path.Combine(Environment.CurrentDirectory, this.relativeGameConfigurationPath);
            }
        }

        /// <summary>
        /// Self-building list of gameconfiguration objects.
        /// 
        /// Requires the AbsoluteGameConfiguration path property to make sense.
        /// If not, returns an empty collection and complains to the logger.
        /// 
        /// If the configuration file exists on the AbsoluteGameConfigurationPath, 
        /// this collection gets built usind the GameConfigurationFactory.
        /// </summary>
        protected ObservableCollection<IGameConfiguration> GameConfigurations
        {
            get
            {
                if (this.gameConfigurations == null)
                {
                    if (!File.Exists(this.AbsoluteGameConfigurationPath))
                    {
                        this.gameConfigurations = new ObservableCollection<IGameConfiguration>();
                        this.EventAggregator.PublishOnUIThread(new LogEntry("Could not find game configuration file at: " + this.AbsoluteGameConfigurationPath, LogEntrySeverity.Error, LogEntrySource.UI, this.AbsoluteGameConfigurationPath));
                    }
                    else
                    {
                        this.gameConfigurations = this.GameConfigurationFactory.BuildModels<IGameConfiguration>(this.AbsoluteGameConfigurationPath);
                    }
                }

                return this.gameConfigurations;
            }
        }

        /// <summary>
        /// TODO:
        /// </summary>
        /// <param name="config"></param>
        protected override void OnConfigLoaded(XDocument config)
        {
            this.relativeGameConfigurationPath = XElementHelper.ReadStringValue(config.Descendants("configuration").First(), "gameConfigurationFile");
        }

        /// <summary>
        /// Builds the ConverterSettings object given by the xml element (XElement)
        /// </summary>
        /// <typeparam name="T">The type of object to create. In this case, it'll be ConverterSettings</typeparam>
        /// <param name="element">The xml node to generate the ConverterSettings object from</param>
        /// <returns>The generated convertersettings object</returns>
        protected override T OnBuildElement<T>(XElement element)
        {
            var name = XElementHelper.ReadStringValue(element, "name");
            var friendlyName = XElementHelper.ReadStringValue(element, "friendlyName");
            var defaultConfigurationFile = XElementHelper.ReadStringValue(element, "defaultConfigurationFile");
            var converterExeName = XElementHelper.ReadStringValue(element, "converterExeName");
            //var userConfigurationFile = XElementHelper.ReadStringValue(element, "userConfigurationFile");
            var additionalInformation = XElementHelper.ReadStringValue(element, "informationText", false);

            // Build game configuration models
            var sourceGameName = XElementHelper.ReadStringValue(element, "sourceGame");
            var targetGameName = XElementHelper.ReadStringValue(element, "targetGame");
            var sourceGame = this.GameConfigurations.FirstOrDefault(g => g.Name.Equals(sourceGameName));
            var targetGame = this.GameConfigurations.FirstOrDefault(g => g.Name.Equals(targetGameName));
            
            // Native export directory.
            //var nativeExportDirectory = XElementHelper.ReadStringValue(element, "nativeParadoxExportDirectory", false);
            //var nativeParadoxExportDirectoryLocationTypeAsString = XElementHelper.ReadStringValue(element, "nativeParadoxExportDirectoryLocationType", false);
            //var nativeParadoxExportDirectoryLocationType = nativeParadoxExportDirectoryLocationTypeAsString.Equals(RelativeFolderLocationRoot.SteamFolder.ToString()) ? RelativeFolderLocationRoot.SteamFolder : RelativeFolderLocationRoot.WindowsUsersFolder;
            //string nativeExportDirectoryTag = XElementHelper.ReadStringValue(element, "nativeParadoxExportDirectoryTag", false);

            //string nativeExportDirectoryAbsolutePath = string.Empty;
            //if (nativeParadoxExportDirectoryLocationType.Equals(RelativeFolderLocationRoot.WindowsUsersFolder))
            //{
            //    nativeExportDirectoryAbsolutePath = DirectoryHelper.GetUsersFolder() + XElementHelper.ReadStringValue(element, "nativeParadoxExportDirectory", false);                
            //}
            //else
            //{
            //    //HACK: This is bad, and needs fixing.
            //    throw new NotSupportedException("The native export directory cannot be a steam subfolder.");
            //}

            string errorMessage = "Could not find game configuration for {0}. Could not find game in " + this.AbsoluteGameConfigurationPath + " with name {1}. ";

            // Build preference categories
            var categories = this.PreferenceCategoryFactory.BuildModels<IPreferenceCategory>(defaultConfigurationFile);

            if (sourceGame == null)
            {
                this.EventAggregator.PublishOnUIThread(new LogEntry(String.Format(errorMessage, "source game", sourceGameName), LogEntrySeverity.Error, LogEntrySource.UI, this.AbsoluteGameConfigurationPath));
            }

            if (targetGame == null)
            {
                this.EventAggregator.PublishOnUIThread(new LogEntry(String.Format(errorMessage, "target game", targetGameName), LogEntrySeverity.Error, LogEntrySource.UI, this.AbsoluteGameConfigurationPath));
            }

            var relativeConverterPath = XElementHelper.ReadStringValue(element, "subfolderName");

            return new ConverterSettings(this.EventAggregator)
            {
                Name = name,
                FriendlyName = friendlyName,
                DefaultConfigurationFile = Path.Combine(Environment.CurrentDirectory, defaultConfigurationFile),
                ConverterExeName = converterExeName,
                SourceGame = sourceGame,
                TargetGame = targetGame,
                AbsoluteConverterPath = Path.Combine(Environment.CurrentDirectory, relativeConverterPath),
                /*NativeParadoxExportDirectoryTag = nativeExportDirectoryTag,
                NativeParadoxExportDirectory = nativeExportDirectoryAbsolutePath,*/
                //UserConfigurationFile = userConfigurationFile 
                Categories = categories,
                AdditionalInformation = additionalInformation
            } as T;
        }
    }
}