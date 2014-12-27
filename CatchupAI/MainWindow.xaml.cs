using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CatchupAI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        const int Q = 50;
        const int leftPad = Q;
        const int topPad = Q;
        Rectangle[,] hexes;
        Game.Stone[,] prevDisplay;
        Game game;

        Brush brushBorder = new SolidColorBrush(Color.FromRgb(0, 0, 0));
        Brush brushEmpty = new SolidColorBrush(Color.FromRgb(150, 150, 255));
        Brush brushP1 = new SolidColorBrush(Color.FromRgb(50, 50, 255));
        Brush brushP2 = new SolidColorBrush(Color.FromRgb(255, 255, 255));

        public MainWindow()
        {
            InitializeComponent();

            hexes = new Rectangle[2 * Game.S - 1, 2 * Game.S - 1];
            prevDisplay = new Game.Stone[2 * Game.S - 1, 2 * Game.S - 1];
            for (int x = 0; x <= 2 * Game.S - 2; ++x)
            {
                for (int y = 0; y <= 2 * Game.S - 2; ++y)
                {
                    if (!Game.inBounds(x, y)) continue;
                    hexes[x, y] = putHex(x, y);
                }
            }

            newGame();
        }

        private void newGame()
        {
            game = new Game();
            display();
        }

        private Rectangle putHex(int x, int y)
        {
            var rect = new System.Windows.Shapes.Rectangle();
            rect.Stroke = brushBorder;
            rect.Fill = brushEmpty;
            rect.HorizontalAlignment = HorizontalAlignment.Left;
            rect.VerticalAlignment = VerticalAlignment.Top;
            rect.Height = rect.Width = Q;
            rect.Margin = new Thickness(
                leftPad + Q * (x + 0.5 * (Game.S - 1 - y)), // left
                topPad + Q * y, // top
                0, // right
                0); // bottom
            rect.Tag = encodePos(x, y);
            rect.MouseUp += new MouseButtonEventHandler(this.hex_MouseUp);

            Grid.SetRow(rect, 1);
            mainGrid.Children.Add(rect);

            return rect;
        }

        private object encodePos(int x, int y)
        {
            return y * (2 * Game.S - 1) + x;
        }

        private Tuple<int, int> decodePos(object tag)
        {
            int t = (int)tag;
            int x = t % (2 * Game.S - 1);
            int y = t / (2 * Game.S - 1);
            return Tuple.Create(x, y);
        }

        private void display()
        {
            currentPlayerCircle.Fill =
                game.isGameOver() ? brushEmpty
                : game.getCurrentPlayer() == 0 ? brushP1
                : brushP2;

            for (int x = 0; x <= 2 * Game.S - 2; ++x)
            {
                for (int y = 0; y <= 2 * Game.S - 2; ++y)
                {
                    if (!Game.inBounds(x, y)) continue;
                    var stone = game.getStone(x, y);
                    if (prevDisplay[x, y] != stone)
                    {
                        prevDisplay[x, y] = stone;

                        switch (stone)
                        {
                            case Game.Stone.Empty:
                                hexes[x, y].Fill = brushEmpty;
                                break;
                            case Game.Stone.Black:
                                hexes[x, y].Fill = brushP1;
                                break;
                            case Game.Stone.White:
                                hexes[x, y].Fill = brushP2;
                                break;
                        }
                    }
                }
            }

            passButton.IsEnabled = game.getMayPass();

            var score = game.getScore();
            var statusText = new StringBuilder();

            if (game.isGameOver())
            {
                statusText.Append("Game is over");
            } else {
                statusText.Append(game.getCurrentPlayer() == 0 ? "Black" : "White");
                statusText.Append(" to play ");
                statusText.Append(game.getRemainingPlays());
                statusText.Append(" stones");
            }
            statusText.Append("\n\n");

            for (int p = 0; p < 2; ++p)
            {
                if (p != 0) statusText.Append("\n");
                statusText.Append(p == 0 ? "Black" : "White");
                statusText.Append(":  ");
                for (int s = 0; s < score[p].Count; ++s)
                {
                    if (s != 0) statusText.Append(", ");
                    statusText.Append(score[p][s]);
                }
            }
            statusBox.Text = statusText.ToString();
        }

        private void newGameMenu_Click(object sender, RoutedEventArgs e)
        {
            newGame();
        }

        private void hex_MouseUp(object sender, MouseButtonEventArgs e)
        {
            var pos = decodePos(((Rectangle)sender).Tag);
            game.userPlay(pos.Item1, pos.Item2);
            display();
        }

        private void passButton_Click(object sender, RoutedEventArgs e)
        {
            game.userPass();
            display();
        }
    }
}
