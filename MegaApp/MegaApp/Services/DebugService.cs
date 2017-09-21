﻿using System;
using Windows.UI.Xaml;
using MegaApp.ViewModels;

namespace MegaApp.Services
{
    class DebugService
    {
        private static DebugSettingsViewModel _debugSettings;
        public static DebugSettingsViewModel DebugSettings
        {
            get
            {
                if (_debugSettings != null) return _debugSettings;
                _debugSettings = new DebugSettingsViewModel();
                return _debugSettings;
            }
        }

        // Timer to count the actions needed to enable/disable the debug mode.
        private static DispatcherTimer _timerDebugMode;

        // Counter for the actions needed to enable/disable the debug mode.
        private static uint _changeStatusActionCounter = 0;

        /// <summary>
        /// Method that should be called when an action required for 
        /// enable/disable the debug mode is done.
        /// </summary>
        public static void ChangeStatusAction()
        {
            if (_changeStatusActionCounter == 0)
            {
                UiService.OnUiThread(() =>
                {
                    if (_timerDebugMode == null)
                    {
                        _timerDebugMode = new DispatcherTimer();
                        _timerDebugMode.Interval = new TimeSpan(0, 0, 5);
                        _timerDebugMode.Tick += (obj, args) => StopDebugModeTimer();
                    }
                    _timerDebugMode.Start();
                });
            }

            _changeStatusActionCounter++;

            if (_changeStatusActionCounter >= 5)
            {
                StopDebugModeTimer();
                DebugSettings.IsDebugMode = !DebugSettings.IsDebugMode;
            }
        }

        /// <summary>
        /// Stops the timer to detect a status change for the debug mode.
        /// </summary>
        private static void StopDebugModeTimer()
        {
            if (_timerDebugMode != null)
                UiService.OnUiThread(() => _timerDebugMode.Stop());
            _changeStatusActionCounter = 0;
        }
    }
}
