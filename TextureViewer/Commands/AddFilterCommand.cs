﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Win32;
using TextureViewer.Models.Filter;
using TextureViewer.Properties;
using TextureViewer.ViewModels;

namespace TextureViewer.Commands
{
    public class AddFilterCommand : ICommand
    {
        private readonly FiltersViewModel viewModel;
        private readonly Models.Models models;

        public AddFilterCommand(Models.Models models, FiltersViewModel viewModel)
        {
            this.viewModel = viewModel;
            this.models = models;
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            var ofd = new OpenFileDialog
            {
                Multiselect = false,
                InitialDirectory = Settings.Default.TonemapperPath
            };

            if (ofd.ShowDialog(models.App.Window) != true) return;
            Settings.Default.TonemapperPath = System.IO.Path.GetDirectoryName(ofd.FileName);

            // load tonemapper
            var disableGl = models.GlContext.Enable();
            try
            {
                // load shader
                var loader = new FilterLoader(ofd.FileName, models.GlContext);
                // create model
                var model = new FilterModel(loader);

                // add to list
                viewModel.AddFilter(model);
            }
            catch (Exception e)
            {
                App.ShowErrorDialog(models.App.Window, e.Message);
            }
            finally
            {
                if(disableGl)
                    models.GlContext.Disable();
            }
        }

        public event EventHandler CanExecuteChanged
        {
            add { }
            remove { }
        }
    }
}
