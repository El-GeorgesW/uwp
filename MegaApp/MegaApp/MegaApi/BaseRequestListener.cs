﻿using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using mega;
using MegaApp.Classes;
using MegaApp.Services;

namespace MegaApp.MegaApi
{
    abstract class BaseRequestListener: MRequestListenerInterface
    {
        #region Properties

        abstract protected string ProgressMessage { get; }
        abstract protected bool ShowProgressMessage { get; }
        abstract protected string ErrorMessage { get; }
        abstract protected string ErrorMessageTitle { get; }
        abstract protected bool ShowErrorMessage { get; }
        abstract protected string SuccessMessage { get; }
        abstract protected string SuccessMessageTitle { get; }
        abstract protected bool ShowSuccesMessage { get; }
        abstract protected bool NavigateOnSucces { get; }
        abstract protected bool ActionOnSucces { get; }
        abstract protected Type NavigateToPage { get; }
        abstract protected NavigationObject NavigationObject { get; }

        #endregion

        #region MRequestListenerInterface

        public virtual void onRequestFinish(MegaSDK api, MRequest request, MError e)
        {
            //Deployment.Current.Dispatcher.BeginInvoke(() =>
            //{
            //    ProgressService.ChangeProgressBarBackgroundColor((Color)Application.Current.Resources["PhoneChromeColor"]);
            //    ProgressService.SetProgressIndicator(false);
            //});

            if (e.getErrorCode() == MErrorType.API_OK)
            {
                if (ShowSuccesMessage)
                {
                    new CustomMessageDialog(
                        SuccessMessageTitle,
                        SuccessMessage,
                        App.AppInformation,
                        MessageDialogButtons.Ok).ShowDialog();
                }

                if (ActionOnSucces)
                    OnSuccesAction(api, request);

                if (NavigateOnSucces)
                    UiService.OnUiThread(() => (Window.Current.Content as Frame).Navigate(NavigateToPage, NavigationObject));
            }
            else if (e.getErrorCode() == MErrorType.API_EBLOCKED) 
            {
                // If the account has been blocked
                api.logout(new LogOutRequestListener(false));

                //Deployment.Current.Dispatcher.BeginInvoke(() =>
                //    NavigateService.NavigateTo(typeof(InitTourPage), NavigationParameter.API_EBLOCKED));
            }
            else if(e.getErrorCode() == MErrorType.API_EOVERQUOTA)
            {
                //Deployment.Current.Dispatcher.BeginInvoke(() =>
                //{
                //    // Stop all upload transfers
                //    api.cancelTransfers((int)MTransferType.TYPE_UPLOAD);
                                                            
                //    // Disable the "camera upload" service if is enabled
                //    if (MediaService.GetAutoCameraUploadStatus())
                //    {
                //        MegaSDK.log(MLogLevel.LOG_LEVEL_INFO, "Disabling CAMERA UPLOADS service (API_EOVERQUOTA)");
                //        MediaService.SetAutoCameraUpload(false);
                //        SettingsService.SaveSetting(SettingsResources.CameraUploadsIsEnabled, false);
                //    }

                //    DialogService.ShowOverquotaAlert();
                //});
            }
            else if (e.getErrorCode() != MErrorType.API_EINCOMPLETE)
            {
                if (ShowErrorMessage)
                {
                    new CustomMessageDialog(
                        ErrorMessageTitle,
                        string.Format(ErrorMessage, e.getErrorString()),
                        App.AppInformation,
                        MessageDialogButtons.Ok).ShowDialog();
                }
            }           
        }

        public virtual void onRequestStart(MegaSDK api, MRequest request)
        {
            //if (!ShowProgressMessage) return;
            //var autoReset = new AutoResetEvent(true);
            //Deployment.Current.Dispatcher.BeginInvoke(() =>
            //{
            //    ProgressService.SetProgressIndicator(true, ProgressMessage);
            //    autoReset.Set();
            //});
            //autoReset.WaitOne();
        }

        public virtual void onRequestTemporaryError(MegaSDK api, MRequest request, MError e)
        {
            //if(DebugService.DebugSettings.IsDebugMode || Debugger.IsAttached)
            //{
                //Deployment.Current.Dispatcher.BeginInvoke(() =>
                //    ProgressService.ChangeProgressBarBackgroundColor((Color)Application.Current.Resources["MegaRedColor"]));
            //}
        }

        public virtual void onRequestUpdate(MegaSDK api, MRequest request)
        {            
            //Deployment.Current.Dispatcher.BeginInvoke(() =>
            //    ProgressService.ChangeProgressBarBackgroundColor((Color)Application.Current.Resources["PhoneChromeColor"]));
        }

        #endregion

        #region Virtual Methods

        protected virtual void OnSuccesAction(MegaSDK api, MRequest request)
        {
            // No standard succes action
        }

        #endregion
    }
}
