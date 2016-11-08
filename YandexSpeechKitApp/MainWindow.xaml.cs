namespace YandexSpeechKitApp
{
    using ApiAiSDK;
    using Microsoft.Win32;
    using System;
    using System.IO;
    using System.Net;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Media.Imaging;
    using System.Xml;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        [DllImport("winmm.dll", EntryPoint = "mciSendStringA", ExactSpelling = true, CharSet = CharSet.Ansi, SetLastError = true)]
        private static extern int record(string lpstrCommand, string lpstrReturnString, int uReturnLength, int hwndCallback);

        private ApiAi apiAi;

        public MainWindow()
        {
            InitializeComponent();

            var config = new AIConfiguration("19702fe30641436e9f364e493cebfb0d", SupportedLanguage.English);
            apiAi = new ApiAi(config);

            img_record.Visibility = Visibility.Visible;
            ellipse.Visibility = Visibility.Hidden;
            lbl_status.Content = "Нажмите кнопку";
        }

        private Stream ReadFileFromResource(string resourceId)
        {
            Assembly a = Assembly.GetExecutingAssembly();
            Stream stream = a.GetManifestResourceStream(resourceId);
            return stream;
        }

        private async void img_record_MouseDown_1(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            lbl_result.Content = "";

            img_record.Source = new BitmapImage(new Uri("microphone.png", UriKind.Relative));
            lbl_status.Content = "Говорите";

            record("open new type waveaudio alias recsound", null, 0, 0);
            record("record recsound", null, 0, 0);

            await Task.Delay(5000);

            string fileName = @"rec.wav";
            record("save recsound " + fileName, null, 0, 0);
            record("close recsound", null, 0, 0);

            img_record.Visibility = Visibility.Hidden;
            ellipse.Visibility = Visibility.Visible;

            lbl_status.Content = "Обработка...";
            await Task.Delay(1000);

            img_record.Source = new BitmapImage(new Uri("recordIcon.png", UriKind.Relative));
            img_record.Visibility = Visibility.Visible;
            ellipse.Visibility = Visibility.Hidden;

            string answer = recognize();

            if (answer != "")
            {
                var response = apiAi.TextRequest(answer);

                if (!response.IsError)
                {
                    if (response.Result.Action == "SayWeather")
                    {
                        lbl_result.Content = "Погода сегодня ухудшится. Будет -10°, возможен снег";
                    }
                }
            }

        }

        private string recognize()
        {
            string _apiKey = "68f69625-3c4f-42ab-9c49-c52a7dafb163";
            string path = "rec.wav";

            byte[] bytes = File.ReadAllBytes(path);

            string postUrl = string.Format("https://asr.yandex.net/asr_xml?" +
            "uuid={0}&key={1}&topic=queries&lang=ru-RU", "01ae13cb744628b58fb536d496daa1e4", _apiKey);

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(postUrl);
            request.Method = "POST";
            request.Host = "asr.yandex.net";
            request.UserAgent = "Mirror";

            request.ContentType = "audio/x-wav";
            request.ContentLength = bytes.Length;

            using (var newStream = request.GetRequestStream())
            {
                newStream.Write(bytes, 0, bytes.Length);
            }

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            string responseToString = "";
            if (response != null)
            {
                var strreader = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
                responseToString = strreader.ReadToEnd();
            }

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(responseToString);

            string xpath = "recognitionResults";
            var nodes = xmlDoc.SelectNodes(xpath);
            if (nodes.Count > 0)
            {
                if (nodes[0].Attributes[0].InnerText == "0")
                {
                    lbl_status.Content = "Не удалось распознать";
                }
                else
                {
                    lbl_status.Content = string.Format("Вы сказали: {0}", nodes[0].InnerText);
                    return nodes[0].InnerText;
                }
            }
            return "";
        }
    }
}
