using System; // EventArgs に必要
using Avalonia.Controls;
using Avalonia.Interactivity;
using P2P; // Peer クラスがこの名前空間に属している場合

namespace Ossom
{
    public partial class BrossomForm : Window
    {
        private Peer[] peers = null;
        private WebServer.HandleWebServer web = null;

        public BrossomForm()
        {
            InitializeComponent();

            // ロード時の初期化処理
            this.Opened += OnWindowOpened;
            this.Closing += OnWindowClosing;
        }

        private void OnWindowOpened(object sender, EventArgs e)
        {
            // 初期化処理（Windows Forms の `Form1_Load` に相当）
            InitializePeers();
        }

        private void OnWindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            foreach (var peer in peers)
            {
                peer.Stop();
            }
            web?.Stop();
        }

        private void InitializePeers()
        {
            // ピアの初期化処理を移行
            peers = new Peer[1]; // 例として 1 つだけ作成
            var config = new PeerConfig(); // 設定の読み込み
            peers[0] = new Peer(config);
            peers[0].Start(0);
        }

        private void OpenCommand_OnExecuted(object sender, RoutedEventArgs e)
        {
            // Open コマンドの処理
        }

        private void ExitCommand_OnExecuted(object sender, RoutedEventArgs e)
        {
            // アプリケーションを終了
            this.Close();
        }
    }
}
