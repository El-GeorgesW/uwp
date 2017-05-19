﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using mega;
using MegaApp.Classes;
using MegaApp.Enums;
using MegaApp.Interfaces;
using MegaApp.MegaApi;
using MegaApp.Services;

namespace MegaApp.ViewModels
{
    public class CloudDriveViewModel: BaseSdkViewModel
    {
        public event EventHandler ClearSelectedItems;
        public event EventHandler DisableSelection;
        public event EventHandler EnableSelection;

        public CloudDriveViewModel()
        {
            InitializeModel();

            this.CopyOrMoveCommand = new RelayCommand(CopyOrMove);
            this.CancelCopyOrMoveCommand = new RelayCommand(CancelCopyOrMove);
            this.AcceptCopyCommand = new RelayCommand(AcceptCopy);
            this.AcceptMoveCommand = new RelayCommand(AcceptMove);
        }

        /// <summary>
        /// Initialize the view model
        /// </summary>
        private void InitializeModel()
        {
            this.CloudDrive = new FolderViewModel(ContainerType.CloudDrive);
            this.RubbishBin = new FolderViewModel(ContainerType.RubbishBin);
            this.CameraUploads = new CameraUploadsViewModel();

            this.CloudDrive.CopyOrMoveEvent += OnCopyOrMove;
            this.RubbishBin.CopyOrMoveEvent += OnCopyOrMove;
            this.CameraUploads.CopyOrMoveEvent += OnCopyOrMove;

            // The Cloud Drive is always the first active folder on initialization
            this.ActiveFolderView = this.CloudDrive;
        }

        #region Commands

        public ICommand CopyOrMoveCommand { get; }
        public ICommand CancelCopyOrMoveCommand { get; }
        public ICommand AcceptCopyCommand { get; }
        public ICommand AcceptMoveCommand { get; }        

        #endregion

        #region Public Methods

        /// <summary>
        /// Add folders to global listener to receive notifications
        /// </summary>
        /// <param name="globalListener">Global notifications listener</param>
        public void Initialize(GlobalListener globalListener)
        {
            //globalListener?.Folders?.Add(this.CloudDrive);
            //globalListener?.Folders?.Add(this.RubbishBin);
            if (globalListener == null) return;
            globalListener.NodeAdded += CloudDrive.OnNodeAdded;
            globalListener.NodeRemoved += CloudDrive.OnNodeRemoved;
            globalListener.NodeAdded += RubbishBin.OnNodeAdded;
            globalListener.NodeRemoved += RubbishBin.OnNodeRemoved;
            globalListener.NodeAdded += CameraUploads.OnNodeAdded;
            globalListener.NodeRemoved += CameraUploads.OnNodeRemoved;
        }

        /// <summary>
        /// Remove folders from global listener
        /// </summary>
        /// <param name="globalListener">Global notifications listener</param>
        public void Deinitialize(GlobalListener globalListener)
        {
            //globalListener?.Folders?.Remove(this.CloudDrive);
            //globalListener?.Folders?.Remove(this.RubbishBin);
            if (globalListener == null) return;
            globalListener.NodeAdded -= CloudDrive.OnNodeAdded;
            globalListener.NodeRemoved -= CloudDrive.OnNodeRemoved;
            globalListener.NodeAdded -= RubbishBin.OnNodeAdded;
            globalListener.NodeRemoved -= RubbishBin.OnNodeRemoved;
            globalListener.NodeAdded -= CameraUploads.OnNodeAdded;
            globalListener.NodeRemoved -= CameraUploads.OnNodeRemoved;
        }

        /// <summary>
        /// Load folders of the view model
        /// </summary>
        public async void LoadFolders()
        {
            if (this.CloudDrive?.FolderRootNode == null)
            {
                this.CloudDrive.FolderRootNode = 
                    NodeService.CreateNew(SdkService.MegaSdk, App.AppInformation, 
                    SdkService.MegaSdk.getRootNode(), this.CloudDrive);
            }

            this.CloudDrive.LoadChildNodes();

            if (this.RubbishBin?.FolderRootNode == null)
            {
                this.RubbishBin.FolderRootNode = 
                    NodeService.CreateNew(SdkService.MegaSdk, App.AppInformation, 
                    SdkService.MegaSdk.getRubbishNode(), this.RubbishBin);
            }

            this.RubbishBin.LoadChildNodes();

            if (this.CameraUploads?.FolderRootNode == null)
            {
                var cameraUploadsNode = await SdkService.GetCameraUploadRootNodeAsync();
                this.CameraUploads.FolderRootNode =
                    NodeService.CreateNew(SdkService.MegaSdk, App.AppInformation,
                        cameraUploadsNode, this.CameraUploads);
            }

            this.CameraUploads.LoadChildNodes();
        }

        /// <summary>
        /// Load all content trees: nodes, shares, contacts
        /// </summary>
        public async void FetchNodes()
        {
            OnUiThread(() => this.CloudDrive?.SetEmptyContentTemplate(true));
            this.CloudDrive?.CancelLoad();

            OnUiThread(() => this.RubbishBin?.SetEmptyContentTemplate(true));
            this.RubbishBin?.CancelLoad();

            OnUiThread(() => this.CameraUploads?.SetEmptyContentTemplate(true));
            this.CameraUploads?.CancelLoad();

            var fetchNodes = new FetchNodesRequestListenerAsync();
            //fetchNodes.ServerBusy += OnServerBusy;
            if (!await fetchNodes.ExecuteAsync(() => this.MegaSdk.fetchNodes(fetchNodes)))
            {
                await DialogService.ShowAlertAsync(
                    ResourceService.AppMessages.GetString("AM_FetchNodesFailed_Title"),
                    ResourceService.AppMessages.GetString("AM_FetchNodesFailed"));
                return;
            }

            var cloudDriveRootNode = this.CloudDrive.FolderRootNode ??
                NodeService.CreateNew(this.MegaSdk, App.AppInformation,
                this.MegaSdk.getRootNode(), this.CloudDrive);
            var rubbishBinRootNode = this.RubbishBin.FolderRootNode ??
                NodeService.CreateNew(this.MegaSdk, App.AppInformation, 
                this.MegaSdk.getRubbishNode(), this.RubbishBin);

            var cameraUploadsNode = await SdkService.GetCameraUploadRootNodeAsync();
            var cameraUploadsRootNode = this.CameraUploads.FolderRootNode ??
                NodeService.CreateNew(this.MegaSdk, App.AppInformation,
                cameraUploadsNode, this.CameraUploads);

            UiService.OnUiThread(() =>
            {
                this.CloudDrive.FolderRootNode = cloudDriveRootNode;
                this.RubbishBin.FolderRootNode = rubbishBinRootNode;
                this.CameraUploads.FolderRootNode = cameraUploadsRootNode;

                LoadFolders();
            });
        }

        #endregion

        #region Private Methods

        private void ResetViewStates()
        {
            CloudDrive.IsMultiSelectActive = false;
            CloudDrive.CurrentViewState = FolderContentViewState.CloudDrive;
            CloudDrive.PreviousViewState = FolderContentViewState.CloudDrive;

            RubbishBin.IsMultiSelectActive = false;
            RubbishBin.CurrentViewState = FolderContentViewState.RubbishBin;
            RubbishBin.PreviousViewState = FolderContentViewState.RubbishBin;

            CameraUploads.IsMultiSelectActive = false;
            CameraUploads.CurrentViewState = FolderContentViewState.CloudDrive;
            CameraUploads.PreviousViewState = FolderContentViewState.CloudDrive;
        }

        private void CopyOrMove() => OnCopyOrMove(this, EventArgs.Empty);

        private void OnCopyOrMove(object sender, EventArgs e)
        {
            if (this.ActiveFolderView.ItemCollection.SelectedItems == null || 
                !this.ActiveFolderView.ItemCollection.HasSelectedItems) return;

            foreach (var node in this.ActiveFolderView.ItemCollection.SelectedItems)
                if (node != null) node.DisplayMode = NodeDisplayMode.SelectedForCopyOrMove;

            this.ActiveFolderView.CopyOrMoveSelectedNodes = this.ActiveFolderView.ItemCollection.SelectedItems.ToList();            
            this.ActiveFolderView.IsMultiSelectActive = false;

            ResetViewStates();

            this.CloudDrive.PreviousViewState = this.CloudDrive.CurrentViewState;
            this.CloudDrive.CurrentViewState = FolderContentViewState.CopyOrMove;
            this.RubbishBin.PreviousViewState = this.RubbishBin.CurrentViewState;
            this.RubbishBin.CurrentViewState = FolderContentViewState.CopyOrMove;
            this.CameraUploads.PreviousViewState = this.CameraUploads.CurrentViewState;
            this.CameraUploads.CurrentViewState = FolderContentViewState.CopyOrMove;

            this.SourceFolderView = this.ActiveFolderView;

            this.DisableSelection?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Reset the variables used in the copy or move actions
        /// </summary>
        private void ResetCopyOrMove()
        {
            SourceFolderView.ItemCollection.SelectedItems.Clear();
            SourceFolderView.CopyOrMoveSelectedNodes.Clear();
            SourceFolderView = null;
            ResetViewStates();
            ClearSelectedItems?.Invoke(this, EventArgs.Empty);
            EnableSelection?.Invoke(this, EventArgs.Empty);
        }

        private void CancelCopyOrMove()
        {
            if (SourceFolderView?.CopyOrMoveSelectedNodes != null)
            {
                foreach (var node in SourceFolderView.CopyOrMoveSelectedNodes)
                    if (node != null) node.DisplayMode = NodeDisplayMode.Normal;
            }

            ResetCopyOrMove();
        }

        private void AcceptCopy()
        {
            // Use a temp variable to avoid InvalidOperationException
            AcceptCopyAction(SourceFolderView.CopyOrMoveSelectedNodes.ToList());
            ResetCopyOrMove();
        }

        private async void AcceptCopyAction(IList<IMegaNode> nodes)
        {
            if (nodes == null || !nodes.Any()) return;

            bool result = true;
            try
            {
                // Fix the new parent node to allow navigation while the nodes are being copied
                var newParentNode = ActiveFolderView.FolderRootNode;
                foreach (var node in nodes)
                {
                    if (node != null)
                    {
                        result = result & (await node.CopyAsync(newParentNode) == NodeActionResult.Succeeded);
                        node.DisplayMode = NodeDisplayMode.Normal;
                    }
                }
            }
            catch (Exception) { result = false; }
            finally
            {
                if (!result)
                {
                    await DialogService.ShowAlertAsync(
                        ResourceService.AppMessages.GetString("AM_CopyFailed_Title"),
                        ResourceService.AppMessages.GetString("AM_CopyFailed"));
                }
            }
        }

        private void AcceptMove()
        {
            // Use a temp variable to avoid InvalidOperationException
            AcceptMoveAction(SourceFolderView.CopyOrMoveSelectedNodes.ToList());
            ResetCopyOrMove();
        }

        private async void AcceptMoveAction(IList<IMegaNode> nodes)
        {
            if (nodes == null || !nodes.Any()) return;

            bool result = true;
            try
            {
                // Fix the new parent node to allow navigation while the nodes are being moved
                var newParentNode = ActiveFolderView.FolderRootNode;
                foreach (var node in nodes)
                {
                    if (node != null)
                    {
                        result = result & (await node.MoveAsync(newParentNode) == NodeActionResult.Succeeded);
                        node.DisplayMode = NodeDisplayMode.Normal;
                    }
                }
            }
            catch (Exception) { result = false; }
            finally
            {
                if (!result)
                {
                    await DialogService.ShowAlertAsync(
                        ResourceService.AppMessages.GetString("AM_MoveFailed_Title"),
                        ResourceService.AppMessages.GetString("AM_MoveFailed"));
                }
            }
        }

        #endregion

        #region Properties

        private FolderViewModel _cloudDrive;
        public FolderViewModel CloudDrive
        {
            get { return _cloudDrive; }
            private set { SetField(ref _cloudDrive, value); }
        }

        private FolderViewModel _rubbishBin;
        public FolderViewModel RubbishBin
        {
            get { return _rubbishBin; }
            private set { SetField(ref _rubbishBin, value); }
        }

        private CameraUploadsViewModel _cameraUploads;
        public CameraUploadsViewModel CameraUploads
        {
            get { return _cameraUploads; }
            private set { SetField(ref _cameraUploads, value); }
        }

        private FolderViewModel _activeFolderView;
        public FolderViewModel ActiveFolderView
        {
            get { return _activeFolderView; }
            set { SetField(ref _activeFolderView, value); }
        }

        /// <summary>
        /// Property needed to store the source folder in a move/copy action 
        /// </summary>
        private FolderViewModel _sourceFolderView;
        public FolderViewModel SourceFolderView
        {
            get { return _sourceFolderView; }
            set { SetField(ref _sourceFolderView, value); }
        }

        #endregion

        #region UiResources

        public string AddFolderText => ResourceService.UiResources.GetString("UI_NewFolder");
        public string CancelText => ResourceService.UiResources.GetString("UI_Cancel");
        public string CloseText => ResourceService.UiResources.GetString("UI_Close");
        public string CloudDriveNameText => ResourceService.UiResources.GetString("UI_CloudDriveName");
        public string CopyOrMoveText => CopyText + "/" + MoveText.ToLower();
        public string CopyText => ResourceService.UiResources.GetString("UI_Copy");
        public string DeselectAllText => ResourceService.UiResources.GetString("UI_DeselectAll");
        public string DownloadText => ResourceService.UiResources.GetString("UI_Download");
        public string EmptyRubbishBinText => ResourceService.UiResources.GetString("UI_EmptyRubbishBin");
        public string MultiSelectText => ResourceService.UiResources.GetString("UI_MultiSelect");
        public string MoveText => ResourceService.UiResources.GetString("UI_Move");
        public string MoveToRubbishBinText => ResourceService.UiResources.GetString("UI_MoveToRubbishBin");
        public string RemoveText => ResourceService.UiResources.GetString("UI_Remove");
        public string RenameText => ResourceService.UiResources.GetString("UI_Rename");
        public string RefreshText => ResourceService.UiResources.GetString("UI_Refresh");        
        public string RubbishBinNameText => ResourceService.UiResources.GetString("UI_RubbishBinName");
        public string SelectAllText => ResourceService.UiResources.GetString("UI_SelectAll");
        public string UploadText => ResourceService.UiResources.GetString("UI_Upload");
        public string CameraUploadsNameText => ResourceService.UiResources.GetString("UI_CameraUploads");

        #endregion

        #region VisualResources

        public string BreadcrumbHomeMegaIcon => ResourceService.VisualResources.GetString("VR_BreadcrumbHomeMegaIcon");
        public string BreadcrumbHomeRubbishBinIcon => ResourceService.VisualResources.GetString("VR_BreadcrumbHomeRubbishBinIcon");
        public string EmptyCloudDrivePathData => ResourceService.VisualResources.GetString("VR_EmptyCloudDrivePathData");
        public string EmptyFolderPathData => ResourceService.VisualResources.GetString("VR_EmptyFolderPathData");
        public string FolderLoadingPathData => ResourceService.VisualResources.GetString("VR_FolderLoadingPathData");

        #endregion
    }
}
