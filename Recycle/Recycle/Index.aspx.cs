﻿using System;
using System.Linq;
using System.IO;
using System.Xml;
using System.Net;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;

namespace Recycle
{
    public partial class Index : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (IsPostBack)
            {
                SubmitAddress();
            }
            GetData();
        }

        protected void GetData()
        {
            // Connect to the database, get the data
            string connString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            string query = "select * from tblLocationData";
            try
            {
                SqlConnection conn = new SqlConnection(connString);
                SqlCommand cmd = new SqlCommand(query, conn);
                conn.Open();

                SqlDataAdapter da = new SqlDataAdapter(cmd);

                var dataTable = new DataTable();

                da.Fill(dataTable);
                conn.Close();
                da.Dispose();
                lblCount.InnerText = "Total of " + dataTable.Rows.Count.ToString() + " requests";
                BuildScript(dataTable);
            }
            catch (SqlException)
            {
                throw new Exception("Error");
            }

        }

        protected void SubmitAddress()
        {
            Address adrs = new Address();
            adrs.FullAddress = txtAddress.Value.ToString();
            try
            {
                adrs.GeoCode();
                //Response.Write(adrs.Latitude + "; " + adrs.Longitude);

                string connString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

                SqlConnection connection = new SqlConnection(connString);

                SqlCommand command = new SqlCommand();
              
                command.Connection = connection; 
                command.CommandType = CommandType.Text;
                command.CommandText = "INSERT into tblLocationData (locFullAddress, locLatitude, locLongitude,locComment) VALUES (@fullAddress, @latitude, @longitude, @comment)";
                command.Parameters.AddWithValue("fullAddress", adrs.FullAddress);
                command.Parameters.AddWithValue("@latitude", adrs.Latitude);
                command.Parameters.AddWithValue("@longitude", adrs.Longitude);
                command.Parameters.AddWithValue("@comment", txtComment.Value.Replace("'", @"\'"));
                command.Parameters.AddWithValue("locFlag", 0);
                try
                {
                    connection.Open();
                    command.ExecuteNonQuery();
                    connection.Close();
                }
                catch (SqlException)
                {
                    throw new Exception("Error");
                }
                
                // clean the boxes
                txtAddress.Value = "";
                txtComment.Value = "";
                lblMsg.Visible = true;
            }
            catch (Exception ex)
            {
                throw new Exception("An Error Occured" + ex.Message);
            }

        }
    

    private void BuildScript(DataTable tbl)
        {
            String Locations = "";

            foreach (DataRow r in tbl.Rows)
            {
                // bypass empty rows	 	
                if (r["locLatitude"].ToString().Trim().Length == 0)
                    continue;

                string Latitude = r["locLatitude"].ToString();
                string Longitude = r["locLongitude"].ToString();
                string FullAddress = r["locFullAddress"].ToString().Replace("'", @"\'");

                // create a line of JavaScript for marker on map for this record 

                //TODO: marker title doesn't want to work... see below
                //string markerOptions = ", { title:'" + FullAddress + "' };";
                //Locations += Environment.NewLine + " map.addOverlay(new GMarker(new GLatLng(" + Latitude + "," + Longitude + "))" + markerOptions + ");";
                Locations += Environment.NewLine + " map.addOverlay(new GMarker(new GLatLng(" + Latitude + "," + Longitude + ")));";
            }

            js.Text = @"<script type='text/javascript'>
                            function initialize() {
                                if (GBrowserIsCompatible()) {
                                    var map = new GMap2(document.getElementById('map_canvas'));
                                    map.setCenter(new GLatLng(39.5254004, -119.8135266), 12); 
                                    " + Locations + @"
                                    map.setUIToDefault();
                                }
                            }
                            </script> ";
        }       
}
    public class Address
    {
        public Address()
        {
        }
        //Properties
        public string Latitude { get; set; }
        public string Longitude { get; set; }
        public string FullAddress { get; set; }

        //The Geocoding here i.e getting the latt/long of address
        public void GeoCode()
        {
            //to Read the Stream
            StreamReader sr = null;

            //The Google Maps API Either return JSON or XML. We are using XML Here
            //Saving the url of the Google API 
            string url = String.Format("http://maps.googleapis.com/maps/api/geocode/xml?address=" + this.FullAddress + "&sensor=false");

            //to Send the request to Web Client 
            WebClient wc = new WebClient();
            try
            {
                sr = new StreamReader(wc.OpenRead(url));
            }
            catch (Exception ex)
            {
                throw new Exception("The Error Occured" + ex.Message);
            }

            try
            {
                XmlTextReader xmlReader = new XmlTextReader(sr);
                bool latread = false;
                bool longread = false;

                while (xmlReader.Read())
                {
                    xmlReader.MoveToElement();
                    switch (xmlReader.Name)
                    {
                        case "lat":

                            if (!latread)
                            {
                                xmlReader.Read();
                                this.Latitude = xmlReader.Value.ToString();
                                latread = true;

                            }
                            break;
                        case "lng":
                            if (!longread)
                            {
                                xmlReader.Read();
                                this.Longitude = xmlReader.Value.ToString();
                                longread = true;
                            }

                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("An Error Occured" + ex.Message);
            }
        }
    }
}