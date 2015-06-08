/*
    Copyright 2013-2014 appPlant UG

    Licensed to the Apache Software Foundation (ASF) under one
    or more contributor license agreements.  See the NOTICE file
    distributed with this work for additional information
    regarding copyright ownership.  The ASF licenses this file
    to you under the Apache License, Version 2.0 (the
    "License"); you may not use this file except in compliance
    with the License.  You may obtain a copy of the License at

     http://www.apache.org/licenses/LICENSE-2.0

    Unless required by applicable law or agreed to in writing,
    software distributed under the License is distributed on an
    "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
    KIND, either express or implied.  See the License for the
    specific language governing permissions and limitations
    under the License.
*/

using System;
using System.Linq;

using Microsoft.Phone.Shell;

using WPCordovaClassLib.Cordova;
using WPCordovaClassLib.Cordova.Commands;
using WPCordovaClassLib.Cordova.JSON;

using De.APPPlant.Cordova.Plugin.LocalNotification;
using Microsoft.Phone.Scheduler;
using System.Collections.Generic;

namespace Cordova.Extension.Commands
{
    /// <summary>
    /// Implementes access to application live tiles
    /// http://msdn.microsoft.com/en-us/library/hh202948(v=VS.92).aspx
    /// </summary>
    public class LocalNotification : BaseCommand
    {
        /// <summary>
        /// Additional Notification type - alarm or reminder
        /// </summary>
        public enum AdditionalNotificationType
        {
            None = 0,
            Alarm = 1,
            Reminder = 2
        } 

        /// <summary>
        /// Informs if the device is ready and the deviceready event has been fired
        /// </summary>
        private bool DeviceReady = false;

        /// <summary>
        /// Informs either the app is running in background or foreground
        /// </summary>
        private bool RunsInBackground = false;

        /// <summary>
        /// An additional way the notification is presented to the user.
        /// </summary>
        private AdditionalNotificationType notificationType = AdditionalNotificationType.Reminder;
        public AdditionalNotificationType NotificationType
        {
            get { return this.notificationType; }
            set { this.notificationType = value; }
        } 

        /// <summary>
        /// Sets application live tile
        /// </summary>
        public void add (string jsonArgs)
        {
            string[] args   = JsonHelper.Deserialize<string[]>(jsonArgs);
            Options options = JsonHelper.Deserialize<Options>(args[0]);
            // Application Tile is always the first Tile, even if it is not pinned to Start.
            ShellTile AppTile = ShellTile.ActiveTiles.First();

            if (AppTile != null)
            {
                // Set the properties to update for the Application Tile
                // Empty strings for the text values and URIs will result in the property being cleared.
                FlipTileData TileData = CreateTileData(options);

                AppTile.Update(TileData);

                try
                {
                    if (this.notificationType == AdditionalNotificationType.Reminder)
                    {
                        //long argsTime = jsonArgs[0].Date;
                        DateTime beginTime = ConvertUnixTimeToLocal(options.Date);
                        
                        if (beginTime < DateTime.Now) { // Check if schedule time is not in past
                            System.Console.WriteLine("Scheduled notification time is in past");
                        }
                        else
                        {
                            Reminder reminder = new Reminder(options.ID);
                            reminder.Title = options.Title;
                            reminder.Content = options.Message;
                            reminder.BeginTime = beginTime;
                            //reminder.ExpirationTime = expirationTime;
                            reminder.RecurrenceType = RecurrenceInterval.None;
                            reminder.NavigationUri = new Uri("/MainPage.xaml", UriKind.Relative);

                            //System.Console.WriteLine("Reminder " + reminder.name + " scheduled at:" + reminder.BeginTime.ToString());

                            if (ScheduledActionService.Find(options.ID) != null)
                            {
                                ScheduledActionService.Remove(options.ID);
                            }

                            ScheduledActionService.Add(reminder);
                        }
                    }
                    else if (this.notificationType == AdditionalNotificationType.Alarm)
                    {
                        Alarm alarm = new Alarm(options.ID);
                        alarm.Content = options.Message;
                        //alarm.Sound = new Uri("/Ringtones/Ring01.wma", UriKind.Relative);
                        alarm.BeginTime = DateTime.Now.AddMinutes(1); ///new DateTime(options.Date);;
                        //alarm.ExpirationTime = expirationTime;
                        alarm.RecurrenceType = RecurrenceInterval.None;

                        if (ScheduledActionService.Find(options.ID) != null)
                        {
                            ScheduledActionService.Remove(options.ID);
                        }

                        ScheduledActionService.Add(alarm);
                    }
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine("Unable to add additional notification");
                }

                FireEvent("trigger", options.ID, options.JSON);
                FireEvent("add", options.ID, options.JSON);

            }

            DispatchCommandResult();
        }

        /// <summary>
        /// Clears the application live tile
        /// </summary>
        public void cancel (string jsonArgs)
        {
            string[] args         = JsonHelper.Deserialize<string[]>(jsonArgs);
            string notificationID = args[0];

            cancelAll(jsonArgs);

            FireEvent("cancel", notificationID, "");
            DispatchCommandResult();

            if (ScheduledActionService.Find(notificationID) != null) {
                ScheduledActionService.Remove(notificationID);
            }
        }

        /// <summary>
        /// Clears the application live tile
        /// </summary>
        public void cancelAll (string jsonArgs)
        {
            // Application Tile is always the first Tile, even if it is not pinned to Start.
            ShellTile AppTile = ShellTile.ActiveTiles.First();

            if (AppTile != null)
            {
                // Set the properties to update for the Application Tile
                // Empty strings for the text values and URIs will result in the property being cleared.
                FlipTileData TileData = new FlipTileData
                {
                    Count                = 0,
                    BackTitle            = "",
                    BackContent          = "",
                    WideBackContent      = "",
                    SmallBackgroundImage = new Uri("appdata:Background.png"),
                    BackgroundImage      = new Uri("appdata:Background.png"),
                    WideBackgroundImage  = new Uri("/Assets/Tiles/FlipCycleTileLarge.png", UriKind.Relative),
                };

                // Update the Application Tile
                AppTile.Update(TileData);

                // Remove all alarms/reminders
                try
                {
                    List<ScheduledNotification> notifications = ScheduledActionService.GetActions<ScheduledNotification>().ToList();
                    foreach (ScheduledNotification notification in notifications)
                    {
                        ScheduledActionService.Remove(notification.Name);
                    }
                }
                catch (Exception ex) { }
            }

            DispatchCommandResult();
        }

        /// <summary>
        /// Checks wether a notification with an ID is scheduled
        /// </summary>
        public void isScheduled (string jsonArgs)
        {
            DispatchCommandResult();
        }

        /// <summary>
        /// Retrieves a list with all currently pending notifications
        /// </summary>
        public void getScheduledIds (string jsonArgs)
        {
            DispatchCommandResult();
        }

        /// <summary>
        /// Checks wether a notification with an ID was triggered
        /// </summary>
        public void isTriggered (string jsonArgs)
        {
            DispatchCommandResult();
        }

        /// <summary>
        /// Retrieves a list with all currently triggered notifications
        /// </summary>
        public void getTriggeredIds (string jsonArgs)
        {
            DispatchCommandResult();
        }

        /// <summary>
        /// Informs that the device is ready and the deviceready event has been fired
        /// </summary>
        public void deviceready (string jsonArgs)
        {
            DeviceReady = true;
        }

        /// <summary>
        /// Creates tile data
        /// </summary>
        private FlipTileData CreateTileData (Options options)
        {
            FlipTileData tile = new FlipTileData();

            // Badge sollte nur gelöscht werden, wenn expliziet eine `0` angegeben wurde
            if (options.Badge != 0)
            {
                tile.Count = options.Badge;
            }

            tile.BackTitle       = options.Title;
            tile.BackContent     = options.ShortMessage;
            tile.WideBackContent = options.Message;

            if (!String.IsNullOrEmpty(options.SmallImage))
            {
                tile.SmallBackgroundImage = new Uri(options.SmallImage, UriKind.RelativeOrAbsolute);
            }

            if (!String.IsNullOrEmpty(options.Image))
            {
                tile.BackgroundImage = new Uri(options.Image, UriKind.RelativeOrAbsolute);
            }

            if (!String.IsNullOrEmpty(options.WideImage))
            {
                tile.WideBackgroundImage = new Uri(options.WideImage, UriKind.RelativeOrAbsolute);
            }

            return tile;
        }

        /// <summary>
        /// Fires the given event.
        /// </summary>
        private void FireEvent (string Event, string Id, string JSON = "")
        {
            string state = ApplicationState();
            string args  = String.Format("\'{0}\',\'{1}\',\'{2}\'", Id, state, JSON);
            string js    = String.Format("window.plugin.notification.local.on{0}({1})", Event, args);

            PluginResult pluginResult = new PluginResult(PluginResult.Status.OK, js);

            pluginResult.KeepCallback = true;

            DispatchCommandResult(pluginResult);
        }

        /// <summary>
        /// Retrieves the application state
        /// Either "background" or "foreground"
        /// </summary>
        private String ApplicationState ()
        {
            return RunsInBackground ? "background" : "foreground";
        }

        private DateTime ConvertUnixTimeToLocal(double unixTime)
        {
            DateTime unixEpochBeginning = new DateTime(1970,1,1,0,0,0,0, DateTimeKind.Utc);
            DateTime localTime = unixEpochBeginning.AddSeconds(unixTime).ToLocalTime();
            return localTime;
        }

        /// <summary>
        /// Occurs when the application is being deactivated.
        /// </summary>
        public override void OnPause (object sender, DeactivatedEventArgs e)
        {
            RunsInBackground = true;
        }

        /// <summary>
        /// Occurs when the application is being made active after previously being put
        /// into a dormant state or tombstoned.
        /// </summary>
        public override void OnResume (object sender, Microsoft.Phone.Shell.ActivatedEventArgs e)
        {
            RunsInBackground = false;
        }
    }
}
