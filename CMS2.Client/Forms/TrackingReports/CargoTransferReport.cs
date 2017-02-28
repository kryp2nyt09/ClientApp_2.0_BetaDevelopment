﻿using CMS2.BusinessLogic;
using CMS2.Entities;
using CMS2.Entities.ReportModel;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMS2.Client.Forms.TrackingReports
{
    public class CargoTransferReport
    {
        public DataTable getData(DateTime date)
        {

            CargoTransferBL cargoTransferBl = new CargoTransferBL();

            List<CargoTransfer> list = cargoTransferBl.GetAll().Where(x => x.RecordStatus == 1 && x.BranchCorpOfficeID == GlobalVars.DeviceBcoId && x.CreatedDate.ToShortDateString() == date.ToShortDateString()).ToList();

            List<CargoTransferViewModel> modelList = Match(list);

            DataTable dt = new DataTable();
            dt.Columns.Add(new DataColumn("No", typeof(string)));
            dt.Columns.Add(new DataColumn("Origin", typeof(string)));
            dt.Columns.Add(new DataColumn("Destination", typeof(string)));
            dt.Columns.Add(new DataColumn("Driver", typeof(string)));
            dt.Columns.Add(new DataColumn("Checker", typeof(string)));
            dt.Columns.Add(new DataColumn("Pieces", typeof(string)));
            dt.Columns.Add(new DataColumn("Plate #", typeof(string)));
            dt.Columns.Add(new DataColumn("Batch", typeof(string)));
            dt.Columns.Add(new DataColumn("AWB", typeof(string)));
            dt.Columns.Add(new DataColumn("QTY", typeof(string)));

            dt.Columns.Add(new DataColumn("CreatedDate", typeof(string)));
            dt.BeginLoadData();
            int ctr = 1;
            foreach (CargoTransferViewModel item in modelList)
            {
                DataRow row = dt.NewRow();
                row[0] = (ctr++).ToString();
                row[1] = item.Origin;
                row[2] = item.Destination;
                row[3] = item.Driver;
                row[4] = item.Checker;
                row[5] = item.Pieces;
                row[6] = item.PlateNo;
                row[7] = item.Batch;
                row[8] = item.AWB;
                row[9] = item.QTY;
                row[10] = item.CreatedDate.ToShortDateString();
                dt.Rows.Add(row);
            }
            dt.EndLoadData();

            return dt;
        }

        public List<int> setWidth()
        {
            List<int> width = new List<int>();
            width.Add(25);
            width.Add(250);
            width.Add(250);
            width.Add(150);
            width.Add(120);
            width.Add(100);
            width.Add(100);
            width.Add(100);

            width.Add(80);
            width.Add(80);
            width.Add(0);
            return width;
        }

        public List<CargoTransferViewModel> Match(List<CargoTransfer> _cargoTransfers) {

            PackageNumberBL _packageNumberService = new PackageNumberBL();
            ShipmentBL shipment = new ShipmentBL();
            List<CargoTransferViewModel> _results = new List<CargoTransferViewModel>();
            foreach (CargoTransfer cargoTransfer in _cargoTransfers)
            {
                CargoTransferViewModel model = new CargoTransferViewModel();
                string _airwaybill = _packageNumberService.GetAll().Find(x => x.PackageNo == cargoTransfer.Cargo).Shipment.AirwayBillNo;
                CargoTransferViewModel isExist = _results.Find(x => x.AWB == _airwaybill);

                if (isExist != null)
                {
                    //isExist.TotalRecieved++;
                    _results.Add(isExist);
                }
                else
                {
                    List<Shipment> list = shipment.GetAll().Where(x => x.AirwayBillNo.Equals(_airwaybill)).ToList();
                    model.Origin = _airwaybill;
                    foreach (Shipment x in list) {
                        model.Origin = x.OriginCity.CityName;
                        model.Destination = x.DestinationCity.CityName;                        
                    }
                    model.Driver = cargoTransfer.Driver;
                    model.Checker = cargoTransfer.Checker;
                    model.Pieces = "";
                    model.PlateNo = cargoTransfer.PlateNo;
                    model.Batch = cargoTransfer.Batch.BatchName;
                    model.AWB = _airwaybill;
                    model.QTY = _cargoTransfers.Count().ToString();
                    model.CreatedDate = cargoTransfer.CreatedDate;
                    _results.Add(model);

                }
            }
            return _results;
        }
    }
}
