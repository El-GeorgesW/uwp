﻿using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
using MegaApp.Enums;
using MegaApp.ViewModels;

namespace MegaApp.Converters
{
    /// <summary>
    /// Class to convert from a viewstate value to a Visibility state (Visible/Collapsed)
    /// </summary>
    public class NodeViewStateToVisibilityConverter: IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var node = value as NodeViewModel;
            var parentFolder = node?.Parent;
            if (parentFolder == null) return Visibility.Collapsed;

            var containerType = parentFolder.Type;
            var command = parameter as string;
            switch (containerType)
            {
                case ContainerType.CloudDrive:
                case ContainerType.CameraUploads:
                    switch (command)
                    {
                        case "preview":
                            return parentFolder.ItemCollection.OnlyOneSelectedItem && node.IsImage ? 
                                Visibility.Visible : Visibility.Collapsed;

                        case "information":
                            return parentFolder.ItemCollection.OnlyOneSelectedItem ?
                                Visibility.Visible : Visibility.Collapsed;

                        case "download":
                        case "copyormove":
                        case "remove":
                            return parentFolder.ItemCollection != null && parentFolder.ItemCollection.HasSelectedItems ?
                                Visibility.Visible : Visibility.Collapsed;

                        case "getlink":
                        case "rename":
                            return parentFolder.ItemCollection.MoreThanOneSelected ? 
                                Visibility.Collapsed : Visibility.Visible;

                        default:
                            return Visibility.Collapsed;
                    }

                case ContainerType.RubbishBin:
                    switch (command)
                    {
                        case "preview":
                            return parentFolder.ItemCollection.OnlyOneSelectedItem && node.IsImage ?
                                Visibility.Visible : Visibility.Collapsed;

                        case "information":
                            return parentFolder.ItemCollection.OnlyOneSelectedItem ?
                                Visibility.Visible : Visibility.Collapsed;

                        case "download":
                        case "copyormove":
                        case "remove":
                            return parentFolder.ItemCollection != null && parentFolder.ItemCollection.HasSelectedItems ?
                                Visibility.Visible : Visibility.Collapsed;
                        
                        case "rename":
                            return parentFolder.ItemCollection.MoreThanOneSelected ?
                                Visibility.Collapsed : Visibility.Visible;
                        default:
                            return Visibility.Collapsed;
                    };

                case ContainerType.InShares:
                case ContainerType.ContactInShares:
                    switch (command)
                    {
                        case "download":
                            return Visibility.Visible;

                        case "remove":
                            return parentFolder.FolderRootNode.HasFullAccessPermissions ?
                                Visibility.Visible : Visibility.Collapsed;

                        case "rename":
                            return parentFolder.ItemCollection.MoreThanOneSelected || !parentFolder.FolderRootNode.HasFullAccessPermissions ?
                                Visibility.Collapsed: Visibility.Visible;

                        default:
                            return Visibility.Collapsed;
                    }

                case ContainerType.OutShares:
                    switch (command)
                    {
                        case "download":
                        case "remove":
                            return Visibility.Visible;

                        case "getlink":
                        case "rename":
                            return parentFolder.ItemCollection.OnlyOneSelectedItem ?
                                Visibility.Visible : Visibility.Collapsed;

                        default:
                            return Visibility.Collapsed;
                    }
                
                case ContainerType.FolderLink:
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return value;
        }
    }
}
