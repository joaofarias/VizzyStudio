using Assets.Scripts.Menu.ListView;
using Assets.Scripts.Vizzy.UI;
using ModApi;
using ModApi.Craft.Program.Instructions;
using ModApi.Craft.Program;
using ModApi.Math;
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;
using System.Collections.Generic;
using ModApi.Common.Extensions;
using ModApi.Craft.Program.Expressions;
using ModApi.Ui;

namespace Assets.Scripts.ViewModels
{
    public class AddReferenceViewModel : ListViewModel
    {
        private AddReferenceDetails _details;
        private VizzyStudioUI _vizzyUI;

        public AddReferenceViewModel(VizzyStudioUI vizzyUI)
        {
            _vizzyUI = vizzyUI;
        }

        public static string GetProgramName(FileInfo file)
        {
            return Utilities.RemoveFileExtension(file.Name);
        }

        public override IEnumerator LoadItems()
        {
            _details = new AddReferenceDetails(ListView.ListViewDetails);
            foreach (FileInfo file in new DirectoryInfo(VizzyUIScript.FlightProgramsFolderPath).GetFiles("*.xml"))
            {
                if (!file.Name.StartsWith("__"))
                {
                    string memoryString = Units.GetMemoryString(file.Length);
                    ListView.CreateItem(GetProgramName(file), memoryString, file);
                }
            }

            yield return new WaitForEndOfFrame();
        }

        public override void OnListViewInitialized(ListViewScript listView)
        {
            base.OnListViewInitialized(listView);

            listView.Title = "Add Reference";
            listView.PrimaryButtonText = "ADD";
            listView.CanDelete = false;
            listView.DisplayType = ListViewScript.ListViewDisplayType.SmallDialog;
            listView.TranslucentBackground = false;
        }

        public override void OnPrimaryButtonClicked(ListViewItemScript selectedItem)
        {
            FileInfo file = selectedItem.ItemModel as FileInfo;
            if (ReferenceManager.Instance.IsReferenceLoaded(file.FullName))
            {
                MessageDialogScript messageDialog = Game.Instance.UserInterface.CreateMessageDialog();
                messageDialog.UseDangerButtonStyle = true;
                messageDialog.MessageText = "This file is already referenced in the program.";
                return;
            }

            Reference reference = ReferenceManager.Instance.LoadReferenceFromFile(file.FullName);
            ReferenceManager.Instance.AddReference(reference);

            ListView.Close();
        }

        public override void UpdateDetails(ListViewItemScript item, Action completeCallback)
        {
            if (item != null)
            {
                _details.UpdateDetails(item.ItemModel as FileInfo);
            }

            completeCallback?.Invoke();
        }

        public class AddReferenceDetails
        {
            private DetailsPropertyScript _createdDate;
            private DetailsPropertyScript _size;

            public AddReferenceDetails(ListViewDetailsScript listViewDetails)
            {
                this._createdDate = listViewDetails.Widgets.AddProperty("Created");
                this._size = listViewDetails.Widgets.AddProperty("Size");
            }

            public void UpdateDetails(FileInfo file)
            {
                this._createdDate.ValueText = RelativeDate(file.LastWriteTime);
                this._size.ValueText = Units.GetMemoryString(file.Length);
            }

            private static string RelativeDate(DateTime d)
            {
                return Utilities.RelativeDate(DateTime.Now, d);
            }
        }
    }
}
