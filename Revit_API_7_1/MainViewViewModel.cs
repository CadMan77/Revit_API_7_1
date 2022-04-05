using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Revit_API_7_1
{
    class MainViewViewModel
    {
        private ExternalCommandData _commandData;

        private UIDocument uidoc;
        private Document doc;

        public List<FamilySymbol> ProjectTitleBlocks { get; } = new List<FamilySymbol>();
        public FamilySymbol SelectedTitleBlock { get; set; }

        public List<View> ProjectViews { get; } = new List<View>();
        public View SelectedView { get; set; }

        public DelegateCommand CreateCommand { get; }

        static readonly string posIntMask = @"^\d+$";
        readonly Regex intRGX = new Regex(posIntMask);

        private int sheetQTY;
        public int SheetQTY
        {
            get => sheetQTY;
            set
            {
                if (value != 0 && intRGX.IsMatch(value.ToString()))
                {
                    sheetQTY = value;
                }
            }
        }

        public string DesignedBy { get; set; }

        public MainViewViewModel(ExternalCommandData commandData)
        {
            _commandData = commandData;
            uidoc = _commandData.Application.ActiveUIDocument;
            doc = uidoc.Document;

            CreateCommand = new DelegateCommand(OnCreateCommand);

            List<FamilySymbol> projectTitleBlocks = new FilteredElementCollector(doc)
                .WhereElementIsElementType()
                .OfCategory(BuiltInCategory.OST_TitleBlocks)
                .Cast<FamilySymbol>()
                .ToList();

            ProjectTitleBlocks = projectTitleBlocks;

            SheetQTY = 1;

            List<View> projectViews = new FilteredElementCollector(doc)
                .WhereElementIsNotElementType()
                .OfCategory(BuiltInCategory.OST_Views)
                .Cast<View>()
                .ToList();

            ProjectViews = projectViews;

            DesignedBy = "Смирнов";
        }
        public EventHandler CloseRequest;

        private void OnCreateCommand()
        {
            if (SelectedTitleBlock == null || sheetQTY < 1)
                return;

            for (int i = 0; i < sheetQTY; i++)
            {
                ViewSheet sheet = null;
                using (Transaction ts = new Transaction(doc, "Sheet Create Transaction"))
                {

                ts.Start();

                    sheet = ViewSheet.Create(doc, SelectedTitleBlock.Id);

                    //sheet.get_Parameter(BuiltInParameter.SHEET_DRAWN_BY).Set(DesignedBy);
                    sheet.get_Parameter(BuiltInParameter.SHEET_DESIGNED_BY).Set(DesignedBy);

                    //XYZ sheetCenterPoint = new XYZ(370/304.8, 300/304.8, 0);

                    //Parameter param = sheet.get_Parameter(BuiltInParameter.SHEET_WIDTH); // ?? https://thebuildingcoder.typepad.com/blog/2010/05/determine-sheet-size.html

                    Element titleBlock = doc.GetElement(new FilteredElementCollector(doc, sheet.Id)
                        .OfCategory(BuiltInCategory.OST_TitleBlocks)
                        .FirstElementId());

                    double sheetWidth = titleBlock.get_Parameter(BuiltInParameter.SHEET_WIDTH).AsDouble();
                    double sheetHeight = titleBlock.get_Parameter(BuiltInParameter.SHEET_HEIGHT).AsDouble();

                    XYZ sheetCenterPoint = new XYZ(sheetWidth/2, sheetHeight/2, 0);

                    //Viewport.Create(doc, sheet.Id, SelectedView.Id, sheetCenterPoint); // вид можно разместить только на одном листе

                    ElementId newViewId = SelectedView.Duplicate(ViewDuplicateOption.Duplicate); 
                    Viewport.Create(doc, sheet.Id, newViewId, sheetCenterPoint);

                ts.Commit();

                //uidoc.ActiveView = View(newViewId);

                }
            }
            RaiseCloseRequest();
        }

        public void RaiseCloseRequest()
        {
            CloseRequest?.Invoke(this, EventArgs.Empty);
        }
    }
}