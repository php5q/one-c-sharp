﻿using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Zhichkin.Hermes.Infrastructure;

namespace Zhichkin.Hermes.UI
{
    public partial class SelectView : UserControl
    {
        public SelectView()
        {
            InitializeComponent();
        }

        private Brush background_brush;
        private void HighlightBackground(object sender)
        {
            UserControl control = sender as UserControl;
            if (control == null) return;
            background_brush = control.Background;
            //control.Background = Brushes.Azure;
            this.Expander_FROM.Background = Brushes.LightGreen;
        }
        private void SetDefaultBackground(object sender)
        {
            UserControl control = sender as UserControl;
            if (control == null) return;
            //control.Background = background_brush;
            this.Expander_FROM.Background = background_brush;
        }
        private void View_DragEnter(object sender, DragEventArgs e)
        {
            object data = e.Data.GetData("Zhichkin.Metadata.Model.Entity");
            if (data == null) return;
            HighlightBackground(sender);
        }
        private void View_DragLeave(object sender, DragEventArgs e)
        {
            object data = e.Data.GetData("Zhichkin.Metadata.Model.Entity");
            if (data == null) return;
            SetDefaultBackground(sender);
        }
        private void View_Drop(object sender, DragEventArgs e)
        {
            object data = e.Data.GetData("Zhichkin.Metadata.Model.Entity");
            if (data == null) return;
            SetDefaultBackground(sender);

            IEntityInfo item = data as IEntityInfo;
            if (item == null) return;
            SelectExpression viewModel = this.DataContext as SelectExpression;
            if (viewModel == null) return;

            //if (e.AllowedEffects == (DragDropEffects.Copy | DragDropEffects.Move))
            //{
            //}
            TableExpression table = new EntityExpression(viewModel)
            {
                Name = item.Name,
                Namespace = item.Namespace,
                Alias = string.Format("{0}_{1}", item.Name, item.TypeCode.ToString())
            };
            foreach (IPropertyInfo p in item.Properties)
            {
                PropertyExpression property = new PropertyExpression(table)
                {
                    Name = p.Name,
                    Alias = p.Name
                };
                table.Fields.Add(property);
            }
            viewModel.Tables.Add(table);
        }
    }
}