using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventManager.Controls
{
    public class FocusEntry : Behavior<Entry> 
    {
        public static readonly BindableProperty IsFocusedProperty =
            BindableProperty.Create(
                nameof(IsFocused),
                typeof(bool),
                typeof(FocusEntry),
                false,
                propertyChanged: OnIsFocusedChanged);

        public bool IsFocused
        {
            get => (bool)GetValue(IsFocusedProperty);
            set => SetValue(IsFocusedProperty, value);
        }

        private static void OnIsFocusedChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is FocusEntry behavior && behavior.AssociatedObject is Entry entry)
            {
                Debug.WriteLine($"[FocusEntry] IsFocused changed: {newValue}");
                if ((bool)newValue)
                {
                    Debug.WriteLine("[FocusEntry] Triggering focus from IsFocused change");
                    behavior.AssociatedObject?.Focus(); 
                }
            }
        }

        private Entry AssociatedObject;

        protected override void OnAttachedTo(Entry entry)
        {
            base.OnAttachedTo(entry);
            AssociatedObject = entry;
            Debug.WriteLine("[FocusEntry] Behavior attached to Entry");

            Debug.WriteLine($"[FocusEntry] IsFocused value on attach: {IsFocused}");

            if (IsFocused)
            {
                Debug.WriteLine("[FocusEntry] Triggering focus on attach");
                entry.Focus();
            }
        }

        protected override void OnDetachingFrom(Entry entry)
        {
            base.OnDetachingFrom(entry);
            AssociatedObject = null;
        }
    }
}
