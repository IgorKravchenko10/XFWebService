using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace XFServiceTest
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        private async void OnCounterClicked(object sender, EventArgs e)
        {
            try
            {
                string address = "https://tcu.base2base.com.ua:9005/api";
                BasicHttpConnection connection = new BasicHttpConnection(address);
                var result = await connection.Post<string[]>("Values", "string", new CancellationToken());
            }
            catch (Exception ex)
            {

            }

        }
    }
}
