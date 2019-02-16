using System;
using System.Windows;
using static System.Int32;

namespace SystemGui
{
    public partial class MainWindow
    {
        private void BtnSearchRealtyRegNumber_OnClick(object sender, RoutedEventArgs e)
        {
            ChangePanel(PanelSearchRealtyRegNumber);
        }

        //2.
        private void BtnSearchRealtyId_OnClick(object sender, RoutedEventArgs e)
        {
            ChangePanel(PanelSearchRealtyId);
        }

        //3.
        private void BtnAddRealty_OnClick(object sender, RoutedEventArgs e)
        {
            ChangePanel(PanelAddRealty);
        }

        private void BtnRemoveRealty_OnClick(object sender, RoutedEventArgs e)
        {
            ChangePanel(PanelRemoveRealty);
        }

        private void BtnUpdateRealty_OnClick(object sender, RoutedEventArgs e)
        {
            ChangePanel(PanelUpdateRealty);
        }

        //1.
        private void BtnSearchRealtyRegNumberSearch_OnClick(object sender, RoutedEventArgs e)
        {
            var reg = TbSearchRealtyRegNumberNumber.Text;
            var name = TbSearchRealtyRegNumberCadastralName.Text;

            if (!CheckEmptyInput(reg, "Register number"))
                return;
            if (!CheckEmptyInput(name, "Cadastral area name"))
                return;

            if (!TryParse(reg, out var regNumber))
            {
                ShowError("Wrong input in register number");
                return;
            }

            StartWatch();
            _dbInterface.SearchRealtyByRegisterNumberAndName(regNumber,name);
        }

        //2.
        private void BtnSearchRealtyIdSearch_OnClick(object sender, RoutedEventArgs e)
        {
            var idText = TbSearchRealtyId.Text;

            if (!CheckEmptyInput(idText, "Id"))
                return;

            if (!TryParse(idText, out var id))
            {
                ShowError("Wrong input in id");
                return;
            }

            StartWatch();
            _dbInterface.SearchRealtyById(id);
        }

        //3.
        private void BtnAddRealtyAdd_OnClick(object sender, RoutedEventArgs e)
        {
            var idText = TbAddRealtyId.Text;
            var name = TbAddRealtyCadAreaName.Text;
            var reg = TbAddRealtyRegisterNumber.Text;
            var desc = TbAddRealtyDescription.Text;

            if (!CheckEmptyInput(idText, "Id"))
                return;
            if (!CheckEmptyInput(name, "Cadastral area name"))
                return;
            if (!CheckEmptyInput(reg, "Register number"))
                return;
            if (!CheckEmptyInput(desc, "Description"))
                return;

            if (!TryParse(idText, out var id))
            {
                ShowError("Wrong input in id");
                return;
            }
            if (!TryParse(reg, out var regNumber))
            {
                ShowError("Wrong input in register number");
                return;
            }
            
            StartWatch();
            _dbInterface.AddRealty(regNumber,id,name,desc);
        }

        //4.
        private void BtnRemoveRealtyRemove_OnClick(object sender, RoutedEventArgs e)
        {
            var reg = TbRemoveRealtyRegNumber.Text;
            var name = TbRemoveRealtyCadname.Text;

            if (!CheckEmptyInput(reg, "Register number"))
                return;
            if (!CheckEmptyInput(name, "Cadastral area name"))
                return;

            if (!TryParse(reg, out var regNumber))
            {
                ShowError("Wrong input in register number");
                return;
            }

            StartWatch();
            _dbInterface.RemoveRealty(regNumber, name);
        }

        //5.
        private void BtnUpdateRealtyUpdate_OnClick(object sender, RoutedEventArgs e)
        {
            var idText = TbUpdateRealtyId.Text;
            var newName = TbUpdateRealtyNewCadAreaName.Text;
            var newDesc = TbUpdateRealtyNewDescription.Text;
            var newReg = TbUpdateRealtyNewRegisterNumber.Text;

            if (!CheckEmptyInput(idText, "Id of realty"))
                return;
            if (!CheckEmptyInput(newName, "Unique name of cadastral area"))
                return;

            var newRegN = -1;
            if (newReg.Length > 0 && !TryParse(newReg, out newRegN) && newRegN > 0)
            {
                ShowError("Wrong input in register number");
                return;
            }

            StartWatch();
            _dbInterface.UpdateRealty(Parse(idText),newName,newDesc,newRegN);
        }


        
    }
}