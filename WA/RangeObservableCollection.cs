namespace WA
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;

    // https://peteohanlon.wordpress.com/2008/10/22/bulk-loading-in-observablecollection/
    public class RangeObservableCollection<T> : ObservableCollection<T>
    {
        private bool _suppressNotification = false;

        public void AddRange(IEnumerable<T> list)
        {
            if (list == null)
            {
                throw new ArgumentNullException("list");
            }

            _suppressNotification = true;
            try
            {
                foreach (T item in list)
                {
                    Add(item);
                }
            }
            finally
            {
                _suppressNotification = false;
            }

            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (!_suppressNotification)
            {
                base.OnCollectionChanged(e);
            }
        }
    }
}
