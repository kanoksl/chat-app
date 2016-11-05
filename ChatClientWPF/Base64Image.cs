using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace ChatClientWPF
{
    public class Base64ImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, 
                              System.Globalization.CultureInfo culture)
        {
            string s = value as string;

            if (s == null)
                return null;

            BitmapImage bi = new BitmapImage();

            bi.BeginInit();
            bi.StreamSource = new MemoryStream(System.Convert.FromBase64String(s));
            bi.EndInit();

            return bi;
        }

        public object ConvertBack(object value, Type targetType, object parameter, 
                                  System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ImageData
    {
        public string B64_UserIcon { get; set; }

        public ImageData()
        {
            this.B64_UserIcon = "PD94bWwgdmVyc2lvbj0iMS4wIiBlbmNvZGluZz0iaXNvLTg4NTktMSI/Pgo8IS0tIEdlbmVyYXRvcjogQWRvYmUgSWxsdXN0cmF0b3IgMTkuMC4wLCBTVkcgRXhwb3J0IFBsdWctSW4gLiBTVkcgVmVyc2lvbjogNi4wMCBCdWlsZCAwKSAgLS0+CjxzdmcgeG1sbnM9Imh0dHA6Ly93d3cudzMub3JnLzIwMDAvc3ZnIiB4bWxuczp4bGluaz0iaHR0cDovL3d3dy53My5vcmcvMTk5OS94bGluayIgdmVyc2lvbj0iMS4xIiBpZD0iTGF5ZXJfMSIgeD0iMHB4IiB5PSIwcHgiIHZpZXdCb3g9IjAgMCA1MTIgNTEyIiBzdHlsZT0iZW5hYmxlLWJhY2tncm91bmQ6bmV3IDAgMCA1MTIgNTEyOyIgeG1sOnNwYWNlPSJwcmVzZXJ2ZSIgd2lkdGg9IjUxMnB4IiBoZWlnaHQ9IjUxMnB4Ij4KPHBhdGggc3R5bGU9ImZpbGw6I0ZGRjFDRDsiIGQ9Ik0zODguNTQyLDc4LjE4M0g1My4wMTRjLTUuMTE0LDAtOS4yNjIsNC4xNDctOS4yNjIsOS4yNjJ2NDYuMzkzTDAsMTU1LjcxMmw0My43NTIsMjEuODc3djQ2LjM5MiAgYzAsNS4xMTUsNC4xNDYsOS4yNjMsOS4yNjIsOS4yNjNoMzM1LjUyOGM1LjExNSwwLDkuMjYyLTQuMTQ3LDkuMjYyLTkuMjYzVjg3LjQ0M0MzOTcuODAyLDgyLjMyOSwzOTMuNjU2LDc4LjE4MywzODguNTQyLDc4LjE4M3oiLz4KPHBhdGggc3R5bGU9ImZpbGw6I0I0RTVFQTsiIGQ9Ik01MTIsMzU2LjI4NWwtNDMuNzUyLTIxLjg3NnYtNDYuMzkzYzAtNS4xMTQtNC4xNDYtOS4yNjItOS4yNjItOS4yNjJIMTIzLjQ1OSAgYy01LjExNSwwLTkuMjYyLDQuMTQ3LTkuMjYyLDkuMjYydjEzNi41MzhjMCw1LjExNSw0LjE0Niw5LjI2Myw5LjI2Miw5LjI2M2gzMzUuNTI4YzUuMTE0LDAsOS4yNjItNC4xNDcsOS4yNjItOS4yNjN2LTQ2LjM5MiAgTDUxMiwzNTYuMjg1eiIvPgo8Zz4KCTxwYXRoIHN0eWxlPSJmaWxsOiM2MDkzOTk7IiBkPSJNMzQzLjA5MiwzODYuODY5SDE2MS4wMzdjLTQuNzE0LDAtOC41MzMtMy44Mi04LjUzMy04LjUzM2MwLTQuNzE0LDMuODItOC41MzMsOC41MzMtOC41MzNoMTgyLjA1NiAgIGM0LjcxNCwwLDguNTMzLDMuODE5LDguNTMzLDguNTMzQzM1MS42MjYsMzgzLjA0OSwzNDcuODA1LDM4Ni44NjksMzQzLjA5MiwzODYuODY5eiIvPgoJPHBhdGggc3R5bGU9ImZpbGw6IzYwOTM5OTsiIGQ9Ik00MjIuNzM3LDM0Mi43NjloLTI2MS43Yy00LjcxNCwwLTguNTMzLTMuODItOC41MzMtOC41MzNzMy44Mi04LjUzMyw4LjUzMy04LjUzM2gyNjEuNyAgIGM0LjcxNCwwLDguNTMzLDMuODIsOC41MzMsOC41MzNTNDI3LjQ0OSwzNDIuNzY5LDQyMi43MzcsMzQyLjc2OXoiLz4KPC9nPgo8Zz4KCTxwYXRoIHN0eWxlPSJmaWxsOiNGRkQyNEQ7IiBkPSJNMjcyLjY0OCwxODYuMjk3SDg5LjAxOWMtNC43MTQsMC04LjUzMy0zLjgyLTguNTMzLTguNTMzczMuODItOC41MzMsOC41MzMtOC41MzNoMTgzLjYyOSAgIGM0LjcxNCwwLDguNTMzLDMuODIsOC41MzMsOC41MzNTMjc3LjM2MSwxODYuMjk3LDI3Mi42NDgsMTg2LjI5N3oiLz4KCTxwYXRoIHN0eWxlPSJmaWxsOiNGRkQyNEQ7IiBkPSJNMzUyLjI5MiwxNDIuMTk3SDg5LjAxOWMtNC43MTQsMC04LjUzMy0zLjgyLTguNTMzLTguNTMzczMuODItOC41MzMsOC41MzMtOC41MzNoMjYzLjI3NCAgIGM0LjcxNCwwLDguNTMzLDMuODIsOC41MzMsOC41MzNTMzU3LjAwNSwxNDIuMTk3LDM1Mi4yOTIsMTQyLjE5N3oiLz4KPC9nPgo8Zz4KPC9nPgo8Zz4KPC9nPgo8Zz4KPC9nPgo8Zz4KPC9nPgo8Zz4KPC9nPgo8Zz4KPC9nPgo8Zz4KPC9nPgo8Zz4KPC9nPgo8Zz4KPC9nPgo8Zz4KPC9nPgo8Zz4KPC9nPgo8Zz4KPC9nPgo8Zz4KPC9nPgo8Zz4KPC9nPgo8Zz4KPC9nPgo8L3N2Zz4K";
        }
    }
}
