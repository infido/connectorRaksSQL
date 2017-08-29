using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Logowanie
{
    public class Logg
    {
        public enum RodzajLogowania { Info, ErrorMSG, Error, Warning };
        public enum MediumLoga { File, Database };
        StreamWriter writer;
        string cashMessage = "";
        
        public Logg()
        {
        }

        public Logg(RodzajLogowania typLog, MediumLoga gdzie, string komunikat)
        {
            setUstawienieLoga(typLog, gdzie, komunikat);
        }

        public Logg(RodzajLogowania typLog, MediumLoga gdzie, string komunikat, bool zapisDobufora)
        {
            setUstawienieLoga(typLog, gdzie, komunikat, zapisDobufora);
        }

        public void setUstawienieLoga(RodzajLogowania typLog, MediumLoga gdzie, string komunikat, bool cash=false)
        {
            if (gdzie == MediumLoga.Database)
            {
                // INNY: zapis loga do bazy danych
                // TODO: dodanie zapisu loga do bazy zamiast pliku do tabeli HIST2
                setZapisLogaDoPliku(typLog, komunikat);
            }
            else
            {
                //zapis loga do pliku
                setZapisLogaDoPliku(typLog, komunikat);
            }
            if (cash)
            {
                cashMessage += komunikat + Environment.NewLine;
            }

        }

        private void setZapisLogaDoPliku(RodzajLogowania typLog, string komunikat)
        {
            
            try
            {
                writer = new StreamWriter(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + "\\ConnectorRaksSQL_" + DateTime.Now.ToShortDateString() + ".log", true);
                writer.WriteLine(" " + typLog + ";" + DateTime.Now.ToString() + ";" + komunikat);

            }
            catch (Exception ex)
            {
                Console.Write("Bład zapisu loga do pliku: " + ex.Message);
                throw;
            }
            finally
            {
                writer.Close();
            }
        }

        public string getBuforKomunikatu()
        {
            return cashMessage;
        }
    }
}
