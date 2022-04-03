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
                .ToList(); // ???

            ProjectTitleBlocks = projectTitleBlocks;

        }
        public EventHandler CloseRequest;

        private void OnCreateCommand()
        {
            if (SelectedTitleBlock == null || sheetQTY < 1)
                return;

            using (Transaction ts = new Transaction(doc, "Sheet  Transaction"))
            {
                ts.Start();

                for (int i = 0; i < sheetQTY; i++)
                    ViewSheet.Create(doc, SelectedTitleBlock.Id);

                ts.Commit();
            }
            RaiseCloseRequest();
        }

        public void RaiseCloseRequest()
        {
            CloseRequest?.Invoke(this, EventArgs.Empty);
        }
    }
}
