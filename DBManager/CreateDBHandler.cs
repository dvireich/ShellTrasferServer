using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBManager
{
    public class CreateDBHandler
    {
        private SqlManager sql;

        public CreateDBHandler()
        {
            sql = new SqlManager();
            sql.Connect();
        }

        ~CreateDBHandler()
        {
            if(sql != null)
                sql.Disconnect();
        }

        public void CreateDataBase()
        {
            sql.CreateDataBase();
        }
    }
}
