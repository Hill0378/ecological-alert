﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Controls;
using ESRI.ArcGIS.DataSourcesRaster;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geoprocessor;
using ESRI.ArcGIS.DataManagementTools;
using System.Text;

namespace ecological_alert
{
    public partial class MosaicDialog : Form
    {
        private AxMapControl _mapControl;
        public List<string> SelectedRasters { get; private set; }
        public string OutputPath { get; private set; }
        public string MosaicMethod { get; private set; }

        public MosaicDialog(AxMapControl axMapControl1)
        {
            InitializeComponent();
            _mapControl = axMapControl1;
            LoadRasterLayers();
            InitializeUI();
        }

        private void InitializeUI()
        {
            cboMethod.Items.AddRange(new string[] {
                "LAST - 后值覆盖前值",
                "FIRST - 前值优先",
                "BLEND - 混合",
                "MEAN - 平均值",
                "MINIMUM - 最小值",
                "MAXIMUM - 最大值"
            });
            cboMethod.SelectedIndex = 0;
        }

        private void LoadRasterLayers()
        {
            clbRasterLayers.Items.Clear();
            for (int i = 0; i < _mapControl.Map.LayerCount; i++)
            {
                ILayer layer = _mapControl.Map.get_Layer(i);
                if (layer is IRasterLayer)
                {
                    clbRasterLayers.Items.Add(layer.Name, true);
                }
            }
        }

        public void ExecuteMosaic(List<string> inputRasters, string outputPath, string mosaicMethod)
        {
            try
            {
                Geoprocessor gp = new Geoprocessor { OverwriteOutput = true };
                MosaicToNewRaster mosaicTool = new MosaicToNewRaster
                {
                    input_rasters = string.Join(";", inputRasters),
                    output_location = Path.GetDirectoryName(outputPath),
                    raster_dataset_name_with_extension = Path.GetFileName(outputPath),
                    pixel_type = "32_BIT_FLOAT",
                    number_of_bands = 1,
                    mosaic_method = mosaicMethod
                };

                gp.Execute(mosaicTool, null);

                if (gp.MessageCount > 0)
                {
                    StringBuilder sb = new StringBuilder();
                for (int i = 0; i < gp.MessageCount; i++)
                    {
                        sb.AppendLine(gp.GetMessage(i)); // 显式传递索引参数
                     }
                    string messages = sb.ToString();
                    MessageBox.Show(messages.Contains("ERROR") ?
                        $"错误:\n{messages}" : "镶嵌成功！",
                        messages.Contains("ERROR") ? "错误" : "成功");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"执行出错: {ex.Message}", "错误");
            }
        }

        public void AddResultLayerToMap(string layerPath)
        {
            try
            {
                if (File.Exists(layerPath))
                {
                    IRasterLayer rasterLayer = new RasterLayer();
                    rasterLayer.CreateFromFilePath(layerPath);
                    _mapControl.AddLayer(rasterLayer);
                    _mapControl.Refresh();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"添加图层失败: {ex.Message}");
            }
        }

        private void Btbrowse_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog dlg = new SaveFileDialog
            {
                Filter = "TIFF文件|*.tif",
                Title = "选择输出位置"
            })
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    outputTextBox.Text = dlg.FileName;
                }
            }
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            List<IRaster> rasters = new List<IRaster>();
            List<string> inputPaths = new List<string>();
            IMap map = _mapControl.Map;

            // 输入验证
            if (clbRasterLayers.CheckedItems.Count < 2)
            {
                MessageBox.Show("请至少选择两个栅格图层", "输入错误");
                return;
            }

            if (string.IsNullOrWhiteSpace(outputTextBox.Text))
            {
                MessageBox.Show("请指定输出路径", "输入错误");
                return;
            }

            if (cboMethod.SelectedIndex == -1)
            {
                MessageBox.Show("请选择镶嵌方法", "输入错误");
                return;
            }

            foreach (string name in clbRasterLayers.CheckedItems)
            {
                for (int i = 0; i < map.LayerCount; i++)
                {
                    if (map.get_Layer(i).Name == name && map.get_Layer(i) is IRasterLayer rasterLayer)
                    {
                        IDataset dataset = rasterLayer as IDataset;
                        if (dataset != null)
                        {
                            string rasterPath = dataset.Workspace.PathName + Path.DirectorySeparatorChar + dataset.Name;
                            Console.WriteLine($"栅格数据路径: {rasterPath}"); // 添加调试输出
                            inputPaths.Add(rasterPath);
                        }
                    }
                }
            }
            string outputDir = Path.GetDirectoryName(OutputPath);
            string outputFileName = Path.GetFileName(OutputPath);
            Console.WriteLine($"输出目录: {outputDir}, 输出文件名: {outputFileName}"); // 添加调试输出

            // 执行镶嵌操作
            MosaicMethod = cboMethod.SelectedItem.ToString().Split(' ')[0];
            OutputPath = outputTextBox.Text;
            ExecuteMosaic(inputPaths, OutputPath, MosaicMethod);

           
        }

        private IRasterLayer FindRasterLayer(string layerName)
        {
            for (int i = 0; i < _mapControl.Map.LayerCount; i++)
            {
                ILayer layer = _mapControl.Map.get_Layer(i);
                if (layer.Name == layerName && layer is IRasterLayer rasterLayer)
                {
                    return rasterLayer;
                }
            }
            return null;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}