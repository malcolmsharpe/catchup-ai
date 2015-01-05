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
        Label[,] labels;
        Game.Stone[,] prevDisplay;
        Game game;
        MCTSAI aiPlayer = null;

        Brush brushBorder = new SolidColorBrush(Color.FromRgb(0, 0, 0));
        Brush brushEmpty = new SolidColorBrush(Color.FromRgb(150, 150, 255));
        Brush brushP1 = new SolidColorBrush(Color.FromRgb(50, 50, 255));
        Brush brushP2 = new SolidColorBrush(Color.FromRgb(255, 255, 255));

        public MainWindow()
        {
            InitializeComponent();

            initGame();

            hexes = new Rectangle[Game.maxX, Game.maxY];
            labels = new Label[Game.maxX, Game.maxY];
            prevDisplay = new Game.Stone[Game.maxX, Game.maxY];
            for (int x = 0; x < Game.maxX; ++x)
            {
                for (int y = 0; y < Game.maxY; ++y)
                {
                    if (!Game.inBounds(x, y)) continue;
                    hexes[x, y] = putHex(x, y);
                    labels[x, y] = putLabel(x, y);
                }
            }

            display();
        }

        private void initGame()
        {
            // TODO: Replace this with configuration.
            var players = new IPlayer[2];
            players[0] = aiPlayer = new MCTSAI();

            game = new Game(players);
        }

        private void newGame()
        {
            initGame();
            display();
        }

        private Thickness hexMargin(int x, int y)
        {
            return new Thickness(
                leftPad + Q * (x + 0.5 * (Game.S - 1 - y)), // left
                topPad + Q * y, // top
                0, // right
                0); // bottom
        }

        private Rectangle putHex(int x, int y)
        {
            var rect = new System.Windows.Shapes.Rectangle();
            rect.Stroke = brushBorder;
            rect.Fill = brushEmpty;
            rect.HorizontalAlignment = HorizontalAlignment.Left;
            rect.VerticalAlignment = VerticalAlignment.Top;
            rect.Height = rect.Width = Q;
            rect.Margin = hexMargin(x, y);
            rect.Tag = Game.toLoc(x, y);
            rect.MouseUp += new MouseButtonEventHandler(this.hex_MouseUp);

            Grid.SetRow(rect, 1);
            mainGrid.Children.Add(rect);

            return rect;
        }

        private Label putLabel(int x, int y)
        {
            var label = new Label();
            label.HorizontalAlignment = HorizontalAlignment.Left;
            label.VerticalAlignment = VerticalAlignment.Top;
            label.IsHitTestVisible = false;
            label.Margin = hexMargin(x, y);
            label.Height = label.Width = Q;
            label.HorizontalContentAlignment = HorizontalAlignment.Center;
            label.VerticalContentAlignment = VerticalAlignment.Center;

            label.FontFamily = new FontFamily("Courier New");
            label.FontSize = 36;
            label.FontWeight = FontWeights.Bold;
            label.Content = "?";

            Panel.SetZIndex(label, 1); // show above hexes

            Grid.SetRow(label, 1);
            mainGrid.Children.Add(label);

            return label;
        }

        private void setLabelColor(int x, int y)
        {
            if (prevDisplay[x, y] == Game.Stone.Black)
            {
                labels[x, y].Foreground = Brushes.White;
            }
            else
            {
                labels[x, y].Foreground = Brushes.Black;
            }
        }

        private void displayMoves(List<int> moves, string content)
        {
            foreach (int loc in moves)
            {
                int x, y;
                game.fromLoc(loc, out x, out y);
                labels[x, y].Content = content;
            }
        }

        private void display()
        {
            currentPlayerCircle.Fill =
                game.isGameOver() ? brushEmpty
                : game.getCurrentPlayer() == 0 ? brushP1
                : brushP2;

            for (int x = 0; x < Game.maxX; ++x)
            {
                for (int y = 0; y < Game.maxY; ++y)
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

                    labels[x, y].Content = "";
                }
            }

            if (aiPlayer != null)
            {
                displayMoves(game.getFreshMoves(), "+");
                displayMoves(aiPlayer.getExpectedResponse(), "?");
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
            int x, y;
            game.fromLoc((int)((Rectangle)sender).Tag, out x, out y);
            game.userPlay(x, y);
            display();
        }

        private void passButton_Click(object sender, RoutedEventArgs e)
        {
            game.userPass();
            display();
        }
    }
}
