using System;
using System.Data;
using System.Text;
using System.Data.SqlClient;
using MesDBAccess.DBUtility;//Please add references
namespace MesDBAccess.DAL
{
    /// <summary>
    /// 数据访问类:ProductOnlineModel
    /// </summary>
    public partial class ProductOnlineDal
    {
        public ProductOnlineDal()
        { }
        #region  BasicMethod
        /// <summary>
        /// 是否存在该记录
        /// </summary>
        public bool Exists(string productID)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select count(1) from ProductOnline");
            strSql.Append(" where productID=@productID ");
            SqlParameter[] parameters = {
					new SqlParameter("@productID", SqlDbType.NVarChar,50)			};
            parameters[0].Value = productID;

            return DbHelperSQL.Exists(strSql.ToString(), parameters);
        }


        /// <summary>
        /// 增加一条数据
        /// </summary>
        public bool Add(MesDBAccess.Model.ProductOnlineModel model)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("insert into ProductOnline(");
            strSql.Append("productID,productCata,batchName,processStepID,palletID,palletBinded,stationID,checkResult,onlineTime,modifyTime,tag1,tag2,tag3,tag4,tag5)");
            strSql.Append(" values (");
            strSql.Append("@productID,@productCata,@batchName,@processStepID,@palletID,@palletBinded,@stationID,@checkResult,@onlineTime,@modifyTime,@tag1,@tag2,@tag3,@tag4,@tag5)");
            SqlParameter[] parameters = {
					new SqlParameter("@productID", SqlDbType.NVarChar,50),
					new SqlParameter("@productCata", SqlDbType.NVarChar,50),
					new SqlParameter("@batchName", SqlDbType.NVarChar,50),
					new SqlParameter("@processStepID", SqlDbType.NVarChar,50),
					new SqlParameter("@palletID", SqlDbType.NVarChar,50),
					new SqlParameter("@palletBinded", SqlDbType.Bit,1),
					new SqlParameter("@stationID", SqlDbType.NVarChar,50),
					new SqlParameter("@checkResult", SqlDbType.NVarChar,50),
					new SqlParameter("@onlineTime", SqlDbType.DateTime),
					new SqlParameter("@modifyTime", SqlDbType.DateTime),
					new SqlParameter("@tag1", SqlDbType.NVarChar,50),
					new SqlParameter("@tag2", SqlDbType.NVarChar,50),
					new SqlParameter("@tag3", SqlDbType.NVarChar,50),
					new SqlParameter("@tag4", SqlDbType.NVarChar,50),
					new SqlParameter("@tag5", SqlDbType.NVarChar,50)};
            parameters[0].Value = model.productID;
            parameters[1].Value = model.productCata;
            parameters[2].Value = model.batchName;
            parameters[3].Value = model.processStepID;
            parameters[4].Value = model.palletID;
            parameters[5].Value = model.palletBinded;
            parameters[6].Value = model.stationID;
            parameters[7].Value = model.checkResult;
            parameters[8].Value = model.onlineTime;
            parameters[9].Value = model.modifyTime;
            parameters[10].Value = model.tag1;
            parameters[11].Value = model.tag2;
            parameters[12].Value = model.tag3;
            parameters[13].Value = model.tag4;
            parameters[14].Value = model.tag5;

            int rows = DbHelperSQL.ExecuteSql(strSql.ToString(), parameters);
            if (rows > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        /// <summary>
        /// 更新一条数据
        /// </summary>
        public bool Update(MesDBAccess.Model.ProductOnlineModel model)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("update ProductOnline set ");
            strSql.Append("productCata=@productCata,");
            strSql.Append("batchName=@batchName,");
            strSql.Append("processStepID=@processStepID,");
            strSql.Append("palletID=@palletID,");
            strSql.Append("palletBinded=@palletBinded,");
            strSql.Append("stationID=@stationID,");
            strSql.Append("checkResult=@checkResult,");
            strSql.Append("onlineTime=@onlineTime,");
            strSql.Append("modifyTime=@modifyTime,");
            strSql.Append("tag1=@tag1,");
            strSql.Append("tag2=@tag2,");
            strSql.Append("tag3=@tag3,");
            strSql.Append("tag4=@tag4,");
            strSql.Append("tag5=@tag5");
            strSql.Append(" where productID=@productID ");
            SqlParameter[] parameters = {
					new SqlParameter("@productCata", SqlDbType.NVarChar,50),
					new SqlParameter("@batchName", SqlDbType.NVarChar,50),
					new SqlParameter("@processStepID", SqlDbType.NVarChar,50),
					new SqlParameter("@palletID", SqlDbType.NVarChar,50),
					new SqlParameter("@palletBinded", SqlDbType.Bit,1),
					new SqlParameter("@stationID", SqlDbType.NVarChar,50),
					new SqlParameter("@checkResult", SqlDbType.NVarChar,50),
					new SqlParameter("@onlineTime", SqlDbType.DateTime),
					new SqlParameter("@modifyTime", SqlDbType.DateTime),
					new SqlParameter("@tag1", SqlDbType.NVarChar,50),
					new SqlParameter("@tag2", SqlDbType.NVarChar,50),
					new SqlParameter("@tag3", SqlDbType.NVarChar,50),
					new SqlParameter("@tag4", SqlDbType.NVarChar,50),
					new SqlParameter("@tag5", SqlDbType.NVarChar,50),
					new SqlParameter("@productID", SqlDbType.NVarChar,50)};
            parameters[0].Value = model.productCata;
            parameters[1].Value = model.batchName;
            parameters[2].Value = model.processStepID;
            parameters[3].Value = model.palletID;
            parameters[4].Value = model.palletBinded;
            parameters[5].Value = model.stationID;
            parameters[6].Value = model.checkResult;
            parameters[7].Value = model.onlineTime;
            parameters[8].Value = model.modifyTime;
            parameters[9].Value = model.tag1;
            parameters[10].Value = model.tag2;
            parameters[11].Value = model.tag3;
            parameters[12].Value = model.tag4;
            parameters[13].Value = model.tag5;
            parameters[14].Value = model.productID;

            int rows = DbHelperSQL.ExecuteSql(strSql.ToString(), parameters);
            if (rows > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// 删除一条数据
        /// </summary>
        public bool Delete(string productID)
        {

            StringBuilder strSql = new StringBuilder();
            strSql.Append("delete from ProductOnline ");
            strSql.Append(" where productID=@productID ");
            SqlParameter[] parameters = {
					new SqlParameter("@productID", SqlDbType.NVarChar,50)			};
            parameters[0].Value = productID;

            int rows = DbHelperSQL.ExecuteSql(strSql.ToString(), parameters);
            if (rows > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        /// <summary>
        /// 批量删除数据
        /// </summary>
        public bool DeleteList(string productIDlist)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("delete from ProductOnline ");
            strSql.Append(" where productID in (" + productIDlist + ")  ");
            int rows = DbHelperSQL.ExecuteSql(strSql.ToString());
            if (rows > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }


        /// <summary>
        /// 得到一个对象实体
        /// </summary>
        public MesDBAccess.Model.ProductOnlineModel GetModel(string productID)
        {

            StringBuilder strSql = new StringBuilder();
            strSql.Append("select  top 1 productID,productCata,batchName,processStepID,palletID,palletBinded,stationID,checkResult,onlineTime,modifyTime,tag1,tag2,tag3,tag4,tag5 from ProductOnline ");
            strSql.Append(" where productID=@productID ");
            SqlParameter[] parameters = {
					new SqlParameter("@productID", SqlDbType.NVarChar,50)			};
            parameters[0].Value = productID;

            MesDBAccess.Model.ProductOnlineModel model = new MesDBAccess.Model.ProductOnlineModel();
            DataSet ds = DbHelperSQL.Query(strSql.ToString(), parameters);
            if (ds.Tables[0].Rows.Count > 0)
            {
                return DataRowToModel(ds.Tables[0].Rows[0]);
            }
            else
            {
                return null;
            }
        }


        /// <summary>
        /// 得到一个对象实体
        /// </summary>
        public MesDBAccess.Model.ProductOnlineModel DataRowToModel(DataRow row)
        {
            MesDBAccess.Model.ProductOnlineModel model = new MesDBAccess.Model.ProductOnlineModel();
            if (row != null)
            {
                if (row["productID"] != null)
                {
                    model.productID = row["productID"].ToString();
                }
                if (row["productCata"] != null)
                {
                    model.productCata = row["productCata"].ToString();
                }
                if (row["batchName"] != null)
                {
                    model.batchName = row["batchName"].ToString();
                }
                if (row["processStepID"] != null)
                {
                    model.processStepID = row["processStepID"].ToString();
                }
                if (row["palletID"] != null)
                {
                    model.palletID = row["palletID"].ToString();
                }
                if (row["palletBinded"] != null && row["palletBinded"].ToString() != "")
                {
                    if ((row["palletBinded"].ToString() == "1") || (row["palletBinded"].ToString().ToLower() == "true"))
                    {
                        model.palletBinded = true;
                    }
                    else
                    {
                        model.palletBinded = false;
                    }
                }
                if (row["stationID"] != null)
                {
                    model.stationID = row["stationID"].ToString();
                }
                if (row["checkResult"] != null)
                {
                    model.checkResult = row["checkResult"].ToString();
                }
                if (row["onlineTime"] != null && row["onlineTime"].ToString() != "")
                {
                    model.onlineTime = DateTime.Parse(row["onlineTime"].ToString());
                }
                if (row["modifyTime"] != null && row["modifyTime"].ToString() != "")
                {
                    model.modifyTime = DateTime.Parse(row["modifyTime"].ToString());
                }
                if (row["tag1"] != null)
                {
                    model.tag1 = row["tag1"].ToString();
                }
                if (row["tag2"] != null)
                {
                    model.tag2 = row["tag2"].ToString();
                }
                if (row["tag3"] != null)
                {
                    model.tag3 = row["tag3"].ToString();
                }
                if (row["tag4"] != null)
                {
                    model.tag4 = row["tag4"].ToString();
                }
                if (row["tag5"] != null)
                {
                    model.tag5 = row["tag5"].ToString();
                }
            }
            return model;
        }

        /// <summary>
        /// 获得数据列表
        /// </summary>
        public DataSet GetList(string strWhere)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select productID,productCata,batchName,processStepID,palletID,palletBinded,stationID,checkResult,onlineTime,modifyTime,tag1,tag2,tag3,tag4,tag5 ");
            strSql.Append(" FROM ProductOnline ");
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            return DbHelperSQL.Query(strSql.ToString());
        }

        /// <summary>
        /// 获得前几行数据
        /// </summary>
        public DataSet GetList(int Top, string strWhere, string filedOrder)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select ");
            if (Top > 0)
            {
                strSql.Append(" top " + Top.ToString());
            }
            strSql.Append(" productID,productCata,batchName,processStepID,palletID,palletBinded,stationID,checkResult,onlineTime,modifyTime,tag1,tag2,tag3,tag4,tag5 ");
            strSql.Append(" FROM ProductOnline ");
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            strSql.Append(" order by " + filedOrder);
            return DbHelperSQL.Query(strSql.ToString());
        }

        /// <summary>
        /// 获取记录总数
        /// </summary>
        public int GetRecordCount(string strWhere)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select count(1) FROM ProductOnline ");
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            object obj = DbHelperSQL.GetSingle(strSql.ToString());
            if (obj == null)
            {
                return 0;
            }
            else
            {
                return Convert.ToInt32(obj);
            }
        }
        /// <summary>
        /// 分页获取数据列表
        /// </summary>
        public DataSet GetListByPage(string strWhere, string orderby, int startIndex, int endIndex)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("SELECT * FROM ( ");
            strSql.Append(" SELECT ROW_NUMBER() OVER (");
            if (!string.IsNullOrEmpty(orderby.Trim()))
            {
                strSql.Append("order by T." + orderby);
            }
            else
            {
                strSql.Append("order by T.productID desc");
            }
            strSql.Append(")AS Row, T.*  from ProductOnline T ");
            if (!string.IsNullOrEmpty(strWhere.Trim()))
            {
                strSql.Append(" WHERE " + strWhere);
            }
            strSql.Append(" ) TT");
            strSql.AppendFormat(" WHERE TT.Row between {0} and {1}", startIndex, endIndex);
            return DbHelperSQL.Query(strSql.ToString());
        }

        /*
        /// <summary>
        /// 分页获取数据列表
        /// </summary>
        public DataSet GetList(int PageSize,int PageIndex,string strWhere)
        {
            SqlParameter[] parameters = {
                    new SqlParameter("@tblName", SqlDbType.VarChar, 255),
                    new SqlParameter("@fldName", SqlDbType.VarChar, 255),
                    new SqlParameter("@PageSize", SqlDbType.Int),
                    new SqlParameter("@PageIndex", SqlDbType.Int),
                    new SqlParameter("@IsReCount", SqlDbType.Bit),
                    new SqlParameter("@OrderType", SqlDbType.Bit),
                    new SqlParameter("@strWhere", SqlDbType.VarChar,1000),
                    };
            parameters[0].Value = "ProductOnline";
            parameters[1].Value = "productID";
            parameters[2].Value = PageSize;
            parameters[3].Value = PageIndex;
            parameters[4].Value = 0;
            parameters[5].Value = 0;
            parameters[6].Value = strWhere;	
            return DbHelperSQL.RunProcedure("UP_GetRecordByPage",parameters,"ds");
        }*/

        #endregion  BasicMethod
        #region  ExtensionMethod
        public DataSet GetListByView(string strWhere)
        {
            StringBuilder strSql = new StringBuilder();
            strSql.Append("select productID,productCata,onlineTime,modifyTime,palletID,palletBinded,batchName,stationName,processStepName,checkResult,positionSeq,positionRow,positionCol");
            strSql.Append(" FROM ViewProduct_PS ");
            if (strWhere.Trim() != "")
            {
                strSql.Append(" where " + strWhere);
            }
            return DbHelperSQL.Query(strSql.ToString());
        }
        public DataSet GetBatchList()
        {
            string strSql = "select distinct batchName from ProductOnline";
            DataSet ds = DbHelperSQL.Query(strSql);
            return ds;
        }
        public DataSet GetPallets(string strWhere)
        {
            string strSql = "select distinct palletID from ViewProduct_PS where palletBinded=1 ";
            if(!string.IsNullOrWhiteSpace(strWhere))
            {
                strSql += string.Format(" and {0}", strWhere);
            }
            DataSet ds = DbHelperSQL.Query(strSql);
            return ds;
        }
        public int GetPalletProductCount(string palletID)
        {
            string sqlStr = "select Count(palletID) from ProductOnline where palletID = '" +palletID+"'";
            DataSet ds = DbHelperSQL.Query(sqlStr);
            int palletProductCount = 0;
            if(ds!=null&&ds.Tables.Count>0)
            {
                palletProductCount = int.Parse(ds.Tables[0].Rows[0][0].ToString());
            }
            return palletProductCount;
        }
        #endregion  ExtensionMethod
    }
}

