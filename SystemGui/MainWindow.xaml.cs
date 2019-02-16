using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
using SystemCore;
using static System.Int32;

namespace SystemGui
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private StackPanel _currentShowingPanel;
        private DatabaseInterface _dbInterface;
        private Stopwatch Watch;
        
        private bool _canWork = false;

        public MainWindow()
        {
            InitializeComponent();
            InitializeDatabaseInterface();
            HideAllPanels();
            _currentShowingPanel = PanelNewData;
            _currentShowingPanel.Visibility = Visibility.Visible;
            InitListView();
            Watch = new Stopwatch();

            EnableAllPanels(false);

            Closing += (sender, args) => _dbInterface.Closing();

            ((GridView) MessageLw.View).Columns[0].Width = 300;
        }

        

        private void StartWatch()
        {
            if (Watch.IsRunning)
            {
                Watch.Stop();
                Watch.Reset();
            }
            Watch.Start();
        }

        private bool CheckEmptyInput(string input, string title)
        {
            if (input.Length == 0)
            {
                MessageBox.Show("Input text can not be empty", title, MessageBoxButton.OK, MessageBoxImage.Warning,
                    MessageBoxResult.OK);
                return false;
            }

            return true;
        }

        private void ShowResults(string data, string resultMessage)
        {
            ResultTextBox.Text = data;
            var text = resultMessage;
            if (Watch.IsRunning)
            {
                Watch.Stop();
                text += $"  {Watch.Elapsed}";
                Watch.Reset();
            }
            MessageLw.Items.Insert(0, text);
        }

        private void ShowError(string message)
        {
            MessageBox.Show(message, "", MessageBoxButton.OK, MessageBoxImage.Error,
                MessageBoxResult.OK);
        }

        private void InitListView()
        {
            var gridView = new GridView();
            MessageLw.View = gridView;
            gridView.Columns.Add(new GridViewColumn
            {
                Header = "Status"
            });
        }

        private void HideAllPanels()
        {
            PanelSearchRealtyRegNumber.Visibility = Visibility.Hidden;
            PanelSearchRealtyId.Visibility = Visibility.Hidden;
            PanelRemoveRealty.Visibility = Visibility.Hidden;
            PanelAddRealty.Visibility = Visibility.Hidden;
            PanelUpdateRealty.Visibility = Visibility.Hidden;
            PanelGenerateData.Visibility = Visibility.Hidden;
            PanelNewData.Visibility = Visibility.Hidden;
        }

        private void EnableAllPanels(bool enabled)
        {
            PanelSearchRealtyRegNumber.IsEnabled = enabled;
            PanelSearchRealtyId.IsEnabled = enabled;
            PanelRemoveRealty.IsEnabled = enabled;
            PanelAddRealty.IsEnabled = enabled;
            PanelUpdateRealty.IsEnabled = enabled;
            PanelGenerateData.IsEnabled = enabled;
            BtnSaveData.IsEnabled = enabled;

            BtnShowBlocksById.IsEnabled = enabled;
            BtnShowBlocksByName.IsEnabled = enabled;
            BtnShowOfBlocksById.IsEnabled = enabled;
            BtnShowOfBlocksByName.IsEnabled = enabled;
            BtnShowRaf.IsEnabled = enabled;
        }

        private void ChangePanel(StackPanel newPanel)
        {
            _currentShowingPanel.Visibility = Visibility.Hidden;
            _currentShowingPanel = newPanel;
            _currentShowingPanel.Visibility = Visibility.Visible;
        }

        private void Tabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source is TabControl)
            {
                _currentShowingPanel.Visibility = Visibility.Hidden;
            }
        }

        private void InitializeDatabaseInterface()
        {
            _dbInterface = new DatabaseInterface();
            _dbInterface.ActionResult += ShowResults;
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void BtnGenerateData_OnClick(object sender, RoutedEventArgs e)
        {
            ChangePanel(PanelGenerateData);
        }

        private void BtnLoadData_OnClick(object sender, RoutedEventArgs e)
        {
            ResultTextBox.Text = "Please wait while data are being loaded";
            EnableAllPanels(true);
            _canWork = true;
            StartWatch();
            _dbInterface.LoadData();
        }

        private void BtnSaveData_OnClick(object sender, RoutedEventArgs e)
        {
            ResultTextBox.Text = "Please wait while data are being saved";
            StartWatch();
            _dbInterface.SaveData();
        }

        private void BtnGenerate_OnClick(object sender, RoutedEventArgs e)
        {
            var cad = TbGenerateDataCadAreas.Text;
            var realties = TbGenerateDataRealties.Text;

            if (!CheckEmptyInput(cad, "Number of cadastral areas"))
                return;
            
            if (!CheckEmptyInput(realties, "Number of realties"))
                return;
            StartWatch();
            _dbInterface.GenerateData(Parse(cad), Parse(realties));
        }

        private void BtnNew_OnClick(object sender, RoutedEventArgs e)
        {
            ChangePanel(PanelNewData);
        }

        private void BtnNewData_OnClick(object sender, RoutedEventArgs e)
        {
            var bFactor = TbNewDataBlockFactor.Text;
            var ofbFactor = TbNewDataOfBlockFactor.Text;
            var blockInRaf = TbNewDataBlockInRaf.Text;
            EnableAllPanels(true);
            _canWork = true;
            _dbInterface.ResetStructures(Parse(bFactor),Parse(ofbFactor), Parse(blockInRaf));
        }

        private void BtnShowBlocksById_OnClick(object sender, RoutedEventArgs e)
        {
            StartWatch();
            _dbInterface.ShowBlocksById();
        }

        private void BtnShowBlocksByName_OnClick(object sender, RoutedEventArgs e)
        {
            StartWatch();
            _dbInterface.ShowBlocksByName();
        }

        private void BtnShowRaf_OnClick(object sender, RoutedEventArgs e)
        {
            StartWatch();
            _dbInterface.ShowBlocksRaf();
        }

        private void BtnShowOfBlocksById_OnClick(object sender, RoutedEventArgs e)
        {
            StartWatch();
            _dbInterface.ShowOverflowBlocksById();
        }

        private void BtnShowOfBlocksByName_OnClick(object sender, RoutedEventArgs e)
        {
            StartWatch();
            _dbInterface.ShowOverflowBlocksByName();
        }

        private void BtnClear_OnClick(object sender, RoutedEventArgs e)
        {
            ResultTextBox.Clear();
        }
    }
}
