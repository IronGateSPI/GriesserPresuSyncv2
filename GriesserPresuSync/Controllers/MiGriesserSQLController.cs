using GriesserPresuSync.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace GriesserPresuSync.Controllers
{
    class MiGriesserSQLController
    {
        public MiGriesserSQLController(GriesserSyncSettings _syncSettings)
        {

        }



        string CreateTableSyntax(string TableName)
        {
            return @"CREATE TABLE {TableName} (
	IdLinea Integer PRIMARY KEY,
	Articulo VARCHAR(255),
	Cliente VARCHAR(255),
	NPresupuesto VARCHAR(255),
	NPersianas Integer,
	TotalSup FLOAT,
	TotalAncho FLOAT,
	TotalLargo FLOAT,
	LargoTapas FLOAT,
	TotalTapas Integer,
	Embalaje VARCHAR(255),
	POS Integer,
	BK FLOAT,
	HL FLOAT,
	Accion Varchar(255),
	TL FLOAT,
	Uni Integer,
	PUnidad FLOAT,
	PUnidad2 FLOAT,
	TEUR FLOAT,
	POS1 Varchar(255),
	Color Varchar(255)
);";

        }
    }
}
