// Copyright 2014 The Rector & Visitors of the University of Virginia
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using SensusService;
using SensusService.DataStores;
using SensusService.DataStores.Local;
using SensusService.DataStores.Remote;
using SensusUI.UiProperties;
using System;
using System.Collections.Generic;
using System.IO;
using Xamarin.Forms;

namespace SensusUI
{
    public class DataStorePage : ContentPage
    {        
        public DataStorePage(Protocol protocol, DataStore dataStore, bool local)
        {
            Title = (local ? "Local" : "Remote") + " Data Store";

            List<StackLayout> stacks = UiProperty.GetPropertyStacks(dataStore);

            StackLayout buttonStack = new StackLayout
            {
                Orientation = StackOrientation.Vertical,
                VerticalOptions = LayoutOptions.FillAndExpand
            };

            stacks.Add(buttonStack);

            if (dataStore.Clearable)
            {
                Button clearButton = new Button
                {
                    Text = "Clear",
                    HorizontalOptions = LayoutOptions.FillAndExpand,
                    FontSize = 20
                };

                clearButton.Clicked += async (o, e) =>
                {
                    if (await DisplayAlert("Clear data from " + dataStore.Name + "?", "This action cannot be undone.", "Clear", "Cancel"))
                        dataStore.Clear();
                };

                buttonStack.Children.Add(clearButton);
            }

            if (local)
            {
                Button shareLocalDataButton = new Button
                {
                    Text = "Share",
                    HorizontalOptions = LayoutOptions.FillAndExpand,
                    FontSize = 20
                };

                shareLocalDataButton.Clicked += async (o, e) =>
                    {
                        LocalDataStore localDataStore = dataStore as LocalDataStore;

                        if(localDataStore.DataCount > 0)
                            await Navigation.PushAsync(new ShareLocalDataStorePage(dataStore as LocalDataStore));
                        else
                            UiBoundSensusServiceHelper.Get(true).FlashNotificationAsync("Local data store contains no data to share.");
                    };

                buttonStack.Children.Add(shareLocalDataButton);
            }

            Button okayButton = new Button
            {
                Text = "OK",
                HorizontalOptions = LayoutOptions.FillAndExpand,
                FontSize = 20
            };

            okayButton.Clicked += async (o, e) =>
                {
                    if (local)
                        protocol.LocalDataStore = dataStore as LocalDataStore;
                    else
                        protocol.RemoteDataStore = dataStore as RemoteDataStore;

                    await Navigation.PopAsync();
                };

            buttonStack.Children.Add(okayButton);

            StackLayout contentLayout = new StackLayout
            {
                Orientation = StackOrientation.Vertical,
                VerticalOptions = LayoutOptions.FillAndExpand
            };

            foreach (StackLayout stack in stacks)
                contentLayout.Children.Add(stack);

            Content = new ScrollView
            {
                Content = contentLayout
            };
        }
    }
}
