using BanjoBotAssets.Exporters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BanjoBotAssets.Reporters
{
    public interface IExportProgressReporter
    {
        public void Report(ExportProgress progress);
    }
}
