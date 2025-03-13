using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Platform;

namespace EventManager.Controls
{
    public class HideSearchBarKeyboard : Behavior<SearchBar>
    {
        protected override void OnAttachedTo(SearchBar searchBar)
        {
            base.OnAttachedTo(searchBar);
            searchBar.SearchButtonPressed += SearchBar_SearchButtonPressed;
        }

        protected override void OnDetachingFrom(SearchBar searchBar)
        {
            base.OnDetachingFrom(searchBar);
            searchBar.SearchButtonPressed -= SearchBar_SearchButtonPressed;
        }

        private async void SearchBar_SearchButtonPressed(object sender, System.EventArgs e)
        {
            if (sender is SearchBar searchBar)
            {
                if (searchBar.IsSoftInputShowing())
                {
                    await searchBar.HideSoftInputAsync(System.Threading.CancellationToken.None);
                }
            }
        }
    }
}
