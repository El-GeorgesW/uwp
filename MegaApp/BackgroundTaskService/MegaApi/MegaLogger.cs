﻿//#define DEBUG
//Uncomment this line to show debug logs even on Release builds
//Only debug levels FATAL, ERROR, WARNING and INFO can be shown in Release builds
//DEBUG and MAX are reserved for Debug builds

using System.Diagnostics;
using mega;

namespace BackgroundTaskService.MegaApi
{
    internal class MegaLogger : MLoggerInterface
    {
        public virtual void log(string time, int loglevel, string source, string message)
        {
            string logLevelString;
            switch((MLogLevel)loglevel)
            {
                case MLogLevel.LOG_LEVEL_DEBUG:
                    logLevelString = " (debug): ";
                    break;
                case MLogLevel.LOG_LEVEL_ERROR:
                    logLevelString = " (error): ";
                    break;
                case MLogLevel.LOG_LEVEL_FATAL:
                    logLevelString = " (fatal): ";
                    break;
                case MLogLevel.LOG_LEVEL_INFO:
                    logLevelString = " (info):  ";
                    break;
                case MLogLevel.LOG_LEVEL_MAX:
                    logLevelString = " (verb):  ";
                    break;
                case MLogLevel.LOG_LEVEL_WARNING:
                    logLevelString = " (warn):  ";
                    break;
                default:
                    logLevelString = " (none):  ";
                    break;
            }

            if(!string.IsNullOrEmpty(source))
            {
                int index = source.LastIndexOf('\\');
                if(index >=0 && source.Length > (index + 1))
                {
                   source = source.Substring(index + 1);
                }
                message += " (" + source + ")"; 
            }

            Debug.WriteLine("{0}{1}BACKGROUND TASK SERVICE - {2}", time, logLevelString, message);
        }
    }
}
