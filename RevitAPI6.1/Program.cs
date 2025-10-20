using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;

namespace DuctCreator
{
    public partial class MainWindow : Window
    {
        private UIDocument _uiDoc;
        private Document _doc;
        private ExternalCommandData _commandData;

        public MainWindow(ExternalCommandData commandData)
        {
            InitializeComponent();
            _commandData = commandData;
            _uiDoc = commandData.Application.ActiveUIDocument;
            _doc = _uiDoc.Document;

            InitializeControls();
        }

        private void InitializeControls()
        {
            try
            {
                var ductTypes = new FilteredElementCollector(_doc)
                    .OfClass(typeof(DuctType))
                    .Cast<DuctType>()
                    .Where(d => d.IsValidObject && !string.IsNullOrEmpty(d.Name))
                    .OrderBy(d => d.Name)
                    .ToList();

                cmbDuctType.ItemsSource = ductTypes;
                cmbDuctType.DisplayMemberPath = "Name";
                if (ductTypes.Count > 0)
                    cmbDuctType.SelectedIndex = 0;

                var levels = new FilteredElementCollector(_doc)
                    .OfClass(typeof(Level))
                    .Cast<Level>()
                    .Where(l => l.IsValidObject)
                    .OrderBy(l => l.Elevation)
                    .ToList();

                cmbLevel.ItemsSource = levels;
                cmbLevel.DisplayMemberPath = "Name";
                if (levels.Count > 0)
                    cmbLevel.SelectedIndex = 0;

                txtMessages.Text = $"Загружено: {ductTypes.Count} типов воздуховодов, {levels.Count} уровней";
            }
            catch (Exception ex)
            {
                txtMessages.Text = $"Ошибка инициализации: {ex.Message}";
            }
        }

        private void BtnPickPoints_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _uiDoc = _commandData.Application.ActiveUIDocument;

                txtMessages.Text = "Выберите первую точку...";
                var point1 = _uiDoc.Selection.PickPoint("Выберите первую точку");
                txtX1.Text = point1.X.ToString("F3");
                txtY1.Text = point1.Y.ToString("F3");
                txtZ1.Text = point1.Z.ToString("F3");

                txtMessages.Text = "Выберите вторую точку...";
                var point2 = _uiDoc.Selection.PickPoint("Выберите вторую точку");
                txtX2.Text = point2.X.ToString("F3");
                txtY2.Text = point2.Y.ToString("F3");
                txtZ2.Text = point2.Z.ToString("F3");

                txtMessages.Text = "Точки выбраны успешно";
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                txtMessages.Text = "Выбор точек отменен";
            }
            catch (Exception ex)
            {
                txtMessages.Text = $"Ошибка выбора точек: {ex.Message}";
            }
        }

        private void BtnCreate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (cmbDuctType.SelectedItem == null || cmbLevel.SelectedItem == null)
                {
                    txtMessages.Text = "Ошибка: Выберите тип воздуховода и уровень";
                    return;
                }

                if (!ValidateInput())
                    return;

                using (Transaction trans = new Transaction(_doc, "Create Duct"))
                {
                    trans.Start();

                    var ductType = cmbDuctType.SelectedItem as DuctType;
                    var level = cmbLevel.SelectedItem as Level;

                    double offset = Convert.ToDouble(txtOffset.Text);
                    double height = Convert.ToDouble(txtHeight.Text);
                    double width = Convert.ToDouble(txtWidth.Text);

                    XYZ point1 = new XYZ(
                        Convert.ToDouble(txtX1.Text),
                        Convert.ToDouble(txtY1.Text),
                        Convert.ToDouble(txtZ1.Text)
                    );

                    XYZ point2 = new XYZ(
                        Convert.ToDouble(txtX2.Text),
                        Convert.ToDouble(txtY2.Text),
                        Convert.ToDouble(txtZ2.Text)
                    );

                    MEPSystemType systemType = new FilteredElementCollector(_doc)
                        .OfClass(typeof(MEPSystemType))
                        .Cast<MEPSystemType>()
                        .FirstOrDefault(m => m.SystemClassification == MEPSystemClassification.SupplyAir);

                    if (systemType == null)
                    {
                        txtMessages.Text = "Ошибка: Не найден тип системы Supply Air";
                        trans.RollBack();
                        return;
                    }

                    Duct duct = Duct.Create(_doc, systemType.Id, ductType.Id, level.Id, point1, point2);

                    if (duct != null)
                    {
                        Parameter offsetParam = duct.get_Parameter(BuiltInParameter.RBS_OFFSET_PARAM);
                        if (offsetParam != null && !offsetParam.IsReadOnly)
                        {
                            offsetParam.Set(offset);
                        }

                        Parameter heightParam = duct.get_Parameter(BuiltInParameter.RBS_CURVE_HEIGHT_PARAM);
                        Parameter widthParam = duct.get_Parameter(BuiltInParameter.RBS_CURVE_WIDTH_PARAM);

                        if (heightParam != null && !heightParam.IsReadOnly)
                            heightParam.Set(height);

                        if (widthParam != null && !widthParam.IsReadOnly)
                            widthParam.Set(width);

                        SetAdditionalParameters(duct);

                        trans.Commit();

                        txtMessages.Text = "✓ Воздуховод успешно создан!";

                        System.Threading.Tasks.Task.Delay(1000).ContinueWith(_ =>
                        {
                            Dispatcher.Invoke(() =>
                            {
                                this.DialogResult = true;
                                this.Close();
                            });
                        });
                    }
                    else
                    {
                        trans.RollBack();
                        txtMessages.Text = "Ошибка: Не удалось создать воздуховод";
                    }
                }
            }
            catch (Exception ex)
            {
                txtMessages.Text = $"Ошибка при создании воздуховода: {ex.Message}";
            }
        }

        private bool ValidateInput()
        {
            try
            {
                Convert.ToDouble(txtOffset.Text);
                Convert.ToDouble(txtHeight.Text);
                Convert.ToDouble(txtWidth.Text);
                Convert.ToDouble(txtX1.Text);
                Convert.ToDouble(txtY1.Text);
                Convert.ToDouble(txtZ1.Text);
                Convert.ToDouble(txtX2.Text);
                Convert.ToDouble(txtY2.Text);
                Convert.ToDouble(txtZ2.Text);

                return true;
            }
            catch
            {
                txtMessages.Text = "Ошибка: Проверьте правильность числовых значений";
                return false;
            }
        }

        private void SetAdditionalParameters(Duct duct)
        {
            try
            {
                Parameter commentsParam = duct.LookupParameter("Comments");
                if (commentsParam != null && !commentsParam.IsReadOnly)
                {
                    commentsParam.Set("Создано через DuctCreator");
                }

                Parameter systemNameParam = duct.get_Parameter(BuiltInParameter.RBS_SYSTEM_NAME_PARAM);
                if (systemNameParam != null && !systemNameParam.IsReadOnly)
                {
                    systemNameParam.Set("Supply Air System");
                }

                Parameter diameterParam = duct.get_Parameter(BuiltInParameter.RBS_CURVE_DIAMETER_PARAM);
                if (diameterParam != null && !diameterParam.IsReadOnly)
                {
                    diameterParam.Set(0.1);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка установки параметров: {ex.Message}");
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }

    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class DuctCreatorCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                MainWindow window = new MainWindow(commandData);
                var result = window.ShowDialog();

                return result == true ? Result.Succeeded : Result.Cancelled;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                TaskDialog.Show("Ошибка", $"Не удалось запустить приложение: {ex.Message}");
                return Result.Failed;
            }
        }
    }
}