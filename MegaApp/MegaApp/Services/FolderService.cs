﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using Windows.Storage;
using mega;

namespace MegaApp.Services
{
    static class FolderService
    {
        /// <summary>
        /// Determines if exists the specified folder
        /// </summary>
        /// <param name="path">Path of the folder</param>
        /// <returns>TRUE if the folder exists or FALSE in other case</returns>
        public static bool FolderExists(string path)
        {  
            return Directory.Exists(path);
        }

        /// <summary>
        /// Gets the number of child folders of the specified folder
        /// </summary>
        /// <param name="path">Path of the folder</param>
        /// <returns>Number of child folders</returns>
        public static int GetNumChildFolders(string path)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path)) return 0;
                return Directory.GetDirectories(path).Length;
            }
            catch (Exception) { return 0; }            
        }

        /// <summary>
        /// Gets the number of child files of the specified folder
        /// </summary>
        /// <param name="path">Path of the folder</param>
        /// <param name="isOfflineFolder">Boolean value which indicates if is an "offline" folder</param>
        /// <returns>Number of child files</returns>
        public static int GetNumChildFiles(string path, bool isOfflineFolder = false)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path)) return 0;

                string[] childFiles = Directory.GetFiles(path);
                if (childFiles == null) return 0;

                int num = 0;
                if (!isOfflineFolder)
                {
                    num = childFiles.Length;
                }
                else
                {
                    foreach (var filePath in childFiles)
                        if (!FileService.IsPendingTransferFile(Path.GetFileName(filePath))) num++;
                }

                return num;
            }
            catch (Exception) { return 0; }
        }

        /// <summary>
        /// Determines if the spedified folder is an empty folder
        /// </summary>
        /// <param name="path">Path of the folder</param>
        /// <returns>TRUE if the folder is empty or FALSE in other case</returns>
        public static bool IsEmptyFolder(string path)
        {
            return (Directory.GetDirectories(path).Count() == 0 && Directory.GetFiles(path).Count() == 0) ? true : false;
        }

        /// <summary>
        /// Creates the specified folder
        /// </summary>
        /// <param name="path">Path of the folder</param>
        /// <returns>TRUE if the folder was created successfully or FALSE in other case</returns>
        public static bool CreateFolder(string path)
        {
            try
            {
                Directory.CreateDirectory(path);
                return true;
            }
            catch(Exception) { return false; }
        }

        /// <summary>
        /// Deletes the specified folder
        /// </summary>
        /// <param name="path">Path of the folder</param>
        /// <param name="recursive">Boolean value which indicates if the deletion should be recursive</param>
        /// <returns>TRUE if the deletion was well or FALSE in other case</returns>
        public static bool DeleteFolder(string path, bool recursive = false)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(path) && Directory.Exists(path))
                    Directory.Delete(path, recursive);
                return true;
            }
            catch (Exception) { return false; }
        }

        /// <summary>
        /// Determines if a folder path contains illegal characters
        /// </summary>
        /// <param name="path">Path of the folder to check</param>
        /// <returns>TRUE if has illegal chars or FALSE in other case</returns>
        public static bool HasIllegalChars(string path)
        {
            var invalidChars = Path.GetInvalidPathChars();
            foreach (var c in invalidChars)
            {
                if (path.Contains(c.ToString())) return true;
            }
            return false;
        }

        /// <summary>
        /// Empties a folder
        /// </summary>
        /// <param name="path">Path of the folder</param>
        public static void Clear(string path)
        {
            try
            {
                IEnumerable<string> foldersToDelete = Directory.GetDirectories(path);
                if (foldersToDelete != null)
                {
                    foreach (var folder in foldersToDelete)
                    {
                        if (folder != null)
                            Directory.Delete(folder, true);
                    }
                }

                FileService.ClearFiles(Directory.GetFiles(path));
            }
            catch (IOException e)
            {
                UiService.OnUiThread(async() =>
                {
                    await DialogService.ShowAlertAsync(
                        ResourceService.AppMessages.GetString("AM_DeleteNodeFailed_Title"),
                        string.Format(ResourceService.AppMessages.GetString("AM_DeleteNodeFailed"), e.Message));
                });
            }
        }

        /// <summary>
        /// Copy a folder to a specified folder and allow rename the destination folder
        /// </summary>
        /// <param name="srcFolderPath">Path of the source folder</param>
        /// <param name="destFolderPath">Path of the destination folder for the copied folder</param>
        /// <param name="folderNewName">New name for the folder</param>
        /// <exception cref="DirectoryNotFoundException"/>
        /// <exception cref="UnauthorizedAccessException"/>
        /// <exception cref="Exception"/>
        public static async Task CopyFolder(string srcFolderPath, string destFolderPath, string folderNewName = null)
        {
            try
            {
                // Get the subfolders for the specified folder.
                DirectoryInfo srcFolder = new DirectoryInfo(srcFolderPath);

                if (!srcFolder.Exists)
                {
                    string errorMessage = "Source folder does not exist or could not be found: " + srcFolderPath;
                    LogService.Log(MLogLevel.LOG_LEVEL_ERROR, errorMessage);
                    throw new DirectoryNotFoundException(errorMessage);
                }

                folderNewName = folderNewName ?? srcFolder.Name;
                destFolderPath = Path.Combine(destFolderPath, folderNewName);
                                
                // If the destination directory doesn't exist, create it.
                if (!Directory.Exists(destFolderPath))
                    Directory.CreateDirectory(destFolderPath);

                // Get the files in the folder and copy them to the new location.
                FileInfo[] files = srcFolder.GetFiles();
                foreach (FileInfo file in files)
                    file.CopyTo(Path.Combine(destFolderPath, file.Name), false);

                // Get the subfolders in the folder and copy them to the new location.
                DirectoryInfo[] subfolders = srcFolder.GetDirectories();
                foreach (DirectoryInfo subfolder in subfolders)
                    await CopyFolder(subfolder.FullName, destFolderPath, subfolder.Name);
            }
            catch (Exception e)
            {
                string errorMessage;
                if (e is UnauthorizedAccessException)
                    errorMessage = "Error copying folder (unauthorized access) \"" + folderNewName + "\": " + e.Message;
                else
                    errorMessage = "Error copying folder \"" + folderNewName + "\": " + e.Message;

                LogService.Log(MLogLevel.LOG_LEVEL_ERROR, errorMessage);
                LogService.Log(MLogLevel.LOG_LEVEL_ERROR, "Source: " + srcFolderPath);
                LogService.Log(MLogLevel.LOG_LEVEL_ERROR, "Destination: " + destFolderPath);
                
                if (e is UnauthorizedAccessException)
                    throw new UnauthorizedAccessException(errorMessage);
                else
                    throw new Exception(errorMessage);
            }
        }

        /// <summary>
        /// Move a folder to a specified folder and allow rename the destination folder.
        /// Copy the folder and remove the source folder if the copy was successful.
        /// </summary>
        /// <param name="srcFolderPath">Path of the source folder</param>
        /// <param name="destFolderPath">Path of the destination folder for the moved folder</param>
        /// <param name="newFolderName">New name for the folder</param>
        /// <exception cref="DirectoryNotFoundException"/>
        /// <exception cref="UnauthorizedAccessException"/>
        /// <exception cref="Exception"/>
        public static async Task MoveFolder(string srcFolderPath, string destFolderPath, string folderNewName = null)
        {
            try
            {
                await CopyFolder(srcFolderPath, destFolderPath, folderNewName);
                DeleteFolder(srcFolderPath, true);
            }
            catch (DirectoryNotFoundException e) { throw new DirectoryNotFoundException(e.Message); }
            catch (UnauthorizedAccessException e) { throw new UnauthorizedAccessException(e.Message); }
            catch (Exception e) { throw new Exception(e.Message); }
        }

        public static async Task<StorageFolder> SelectFolder()
        {
            try
            {
                var folderPicker = new FolderPicker
                {
                    ViewMode = PickerViewMode.List,
                    SuggestedStartLocation = PickerLocationId.Downloads,
                    CommitButtonText = ResourceService.UiResources.GetString("UI_Download")
                };
                folderPicker.FileTypeFilter.Add("*");

                var folder = await folderPicker.PickSingleFolderAsync();

                if(folder != null)
                    Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.Add(folder);

                return folder;
            }
            catch (Exception e)
            {
                UiService.OnUiThread(async() =>
                {
                    await DialogService.ShowAlertAsync(
                        ResourceService.AppMessages.GetString("AM_SelectFolderFailed_Title"),
                        string.Format(ResourceService.AppMessages.GetString("AM_SelectFolderFailed"), e.Message));
                });

                return null;
            }
        }
    }    
}