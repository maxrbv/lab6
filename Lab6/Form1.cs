using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Npgsql;

namespace Lab6
{
    public partial class Form1 : Form
    {
        private string connectstring = "host = localhost; username = postgres; password = maxrbv; database = cybersport_bd";

        public Form1()
        {
            InitializeComponent();
            InitalizeGridView();
        }

        void InitalizeGridView()
        {
            GridView.Rows.Clear();
            GridView.Columns.Clear();
            DataTable country = DataFill("select country_name from country");
            GridView.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "player_id",
                Visible = false
            });
            GridView.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "player_nickname",
                HeaderText = "nickname"
            });
            GridView.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "player_rating",
                HeaderText = "rating",
                ValueType = typeof(uint)
            });
            GridView.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "country_id",
                Visible = false
            }); 
            GridView.Columns.Add(new DataGridViewComboBoxColumn 
            {
                Name = "country_name",
                HeaderText = "country",
                DataSource = country,
                DisplayMember = "country_name",
            });
            GridView.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "cyberteam_id",
                Visible = false
            });
            GridView.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "cyberteam_name",
                HeaderText = "team name"
            });
            GridView.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "cyberteam_regdate",
                HeaderText = "team reg date",
                ValueType = typeof(DateTime)
            });
            GridView.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "cyberteam_tier",
                HeaderText = "team tier",
                ValueType = typeof(char)
            });

            var connect = new NpgsqlConnection(connectstring);
            connect.Open();
            var command_reader = new NpgsqlCommand()
            {
                Connection = connect,
                CommandText = "select player_id, player_nickname, player_rating, country_id, country_name, cyberteam_id, cyberteam_name, cyberteam_regdate, cyberteam_tier from player inner join country c on player.player_counrty = c.country_id inner join team_name tn on player.team_id_fk = tn.cyberteam_id;"
            };
            var reader = command_reader.ExecuteReader();
            while (reader.Read())
            {
                GridView.Rows.Add(reader["player_id"], reader["player_nickname"], reader["player_rating"], reader["country_id"], reader["country_name"], reader["cyberteam_id"], reader["cyberteam_name"], reader["cyberteam_regdate"], reader["cyberteam_tier"]);
            }
            connect.Close();
            FillTag(GridView);

        }
        void FillTag(DataGridView dataGridView) // не для добавления 
        {
            foreach (DataGridViewRow row in dataGridView.Rows)
            {
                if (!row.IsNewRow)
                {
                    List<object> values = new List<object>();
                    foreach (DataGridViewCell cell in row.Cells)
                        values.Add(cell.Value);
                    row.Tag = values;
                }
            }
        }
        DataTable DataFill(string query)
        {
            var connect = new NpgsqlConnection(connectstring);
            connect.Open();
            var command = new NpgsqlCommand()
            {
                Connection = connect,
                CommandText = query
            };
            DataTable result = new DataTable();
            result.Load(command.ExecuteReader());
            connect.Close();
            return result;
        }

        private void GridView_RowValidating(object sender, DataGridViewCellCancelEventArgs e)
        {
            var row = GridView.Rows[e.RowIndex];
            if (!e.Cancel)
            {
                if (GridView.IsCurrentRowDirty)
                {
                    foreach (DataGridViewCell cell in row.Cells)
                        if (cell.ColumnIndex != 8 && cell.ColumnIndex != 0 && cell.ColumnIndex != 3 && cell.ColumnIndex != 5)
                            if (cell.Value == null)
                            {
                                MessageBox.Show("dsf"); return;
                            } 
                    var connect = new NpgsqlConnection(connectstring);
                    connect.Open();
                    var command_search_country = new NpgsqlCommand()
                    {
                        Connection = connect
                    };
                    command_search_country.Parameters.AddWithValue("@country_name", row.Cells["country_name"].Value);
                    command_search_country.CommandText = "select country_id from country where country_name = @country_name";
                    int country_id = (int)command_search_country.ExecuteScalar();
                    var command_search_team = new NpgsqlCommand()
                    {
                        Connection = connect
                    };
                    command_search_team.Parameters.AddWithValue("@cyberteam_name", row.Cells["cyberteam_name"].Value);
                    command_search_team.CommandText = "select cyberteam_id from team_name where cyberteam_name = @cyberteam_name";
                    int? team_id = (int?)command_search_team.ExecuteScalar();
                    int? player_id = null;

                    if (row.Tag == null)
                    {
                        if (team_id == null) 
                        {
                            var command_insert_team = new NpgsqlCommand()
                            {
                                Connection = connect
                            };
                            command_insert_team.CommandText = "insert into team_name(cyberteam_name, cyberteam_regdate, cyberteam_tier) values (@cyberteam_name, @cyberteam_regdate, @cyberteam_tier) returning cyberteam_id";
                            command_insert_team.Parameters.AddWithValue("@cyberteam_name", row.Cells["cyberteam_name"].Value);
                            command_insert_team.Parameters.AddWithValue("@cyberteam_regdate", row.Cells["cyberteam_regdate"].Value);
                            command_insert_team.Parameters.AddWithValue("@cyberteam_tier", row.Cells["cyberteam_tier"].Value ?? DBNull.Value);
                            team_id = (int?)command_insert_team.ExecuteScalar();
                        }
                        var command_insert_player = new NpgsqlCommand()
                        {
                            Connection = connect
                        };
                        command_insert_player.CommandText = "insert into player(player_nickname, player_rating, team_id_fk, player_counrty) values (@player_nickname, @player_rating, @team_id_fk, @player_counrty) returning player_id";
                        command_insert_player.Parameters.AddWithValue("@player_nickname", row.Cells["player_nickname"].Value);
                        command_insert_player.Parameters.AddWithValue("@player_rating", (int)(uint)row.Cells["player_rating"].Value);
                        command_insert_player.Parameters.AddWithValue("@team_id_fk", team_id);
                        command_insert_player.Parameters.AddWithValue("@player_counrty", country_id);
                        player_id = (int?)command_insert_player.ExecuteScalar();
                        row.Cells["player_id"].Value = player_id;
                        row.Cells["country_id"].Value = country_id;
                        row.Cells["cyberteam_id"].Value = team_id;
                    }
                    else
                    {
                        if (team_id == null)
                        {
                            var command_insert_team = new NpgsqlCommand()
                            {
                                Connection = connect
                            };
                            command_insert_team.CommandText = "insert into team_name(cyberteam_name, cyberteam_regdate, cyberteam_tier) values (@cyberteam_name, @cyberteam_regdate, @cyberteam_tier) returning cyberteam_id";
                            command_insert_team.Parameters.AddWithValue("@cyberteam_name", row.Cells["cyberteam_name"].Value);
                            command_insert_team.Parameters.AddWithValue("@cyberteam_regdate", row.Cells["cyberteam_regdate"].Value);
                            command_insert_team.Parameters.AddWithValue("@cyberteam_tier", row.Cells["cyberteam_tier"].Value ?? DBNull.Value);
                            team_id = (int?)command_insert_team.ExecuteScalar();
                        }

                        else
                        {
                            var command_update_team = new NpgsqlCommand()
                            {
                                Connection = connect
                            };
                            command_update_team.CommandText = "update team_name set cyberteam_name = @cyberteam_name, cyberteam_regdate = @cyberteam_regdate, cyberteam_tier = @cyberteam_tier where cyberteam_id = @cyberteam_id";
                            command_update_team.Parameters.AddWithValue("@cyberteam_name", row.Cells["cyberteam_name"].Value);
                            command_update_team.Parameters.AddWithValue("@cyberteam_regdate", row.Cells["cyberteam_regdate"].Value);
                            command_update_team.Parameters.AddWithValue("@cyberteam_tier", row.Cells["cyberteam_tier"].Value ?? DBNull.Value);
                            command_update_team.Parameters.AddWithValue("@cyberteam_id", team_id);
                            command_update_team.ExecuteNonQuery();
                        }

                        var command_update_player = new NpgsqlCommand()
                        {
                            Connection = connect
                        };
                        command_update_player.CommandText = "update player set player_nickname = @player_nickname, player_rating = @player_rating, player_counrty = @player_counrty, team_id_fk = @team_id_fk where player_id = @player_id";
                        command_update_player.Parameters.AddWithValue("@player_nickname", row.Cells["player_nickname"].Value);
                        command_update_player.Parameters.AddWithValue("@player_rating", row.Cells["player_rating"].Value is uint ? (int)(uint)row.Cells["player_rating"].Value : (int)row.Cells["player_rating"].Value);
                        command_update_player.Parameters.AddWithValue("@player_id", row.Cells["player_id"].Value);
                        command_update_player.Parameters.AddWithValue("@player_counrty", country_id);
                        command_update_player.Parameters.AddWithValue("@team_id_fk", team_id);
                        command_update_player.ExecuteNonQuery();
                        UpdateInfoTeam(row, (int)team_id);
                        row.Cells["country_id"].Value = country_id;
                        row.Cells["cyberteam_id"].Value = team_id;
                        
                    }
                    GridView.Rows[e.RowIndex].Cells["cyberteam_regdate"].ReadOnly = false;
                    GridView.Rows[e.RowIndex].Cells["cyberteam_tier"].ReadOnly = false;
                    List<object> values = new List<object>();
                    foreach (DataGridViewCell cell in row.Cells)
                        values.Add(cell.Value);
                    row.Tag = values;
                }
            }
        }

        void UpdateInfoTeam(DataGridViewRow row, int team_id)
        {
            if (!row.Cells["cyberteam_regdate"].Value.Equals(((List<object>)row.Tag)[7]) || !row.Cells["cyberteam_regdate"].Value.Equals(((List<object>)row.Tag)[8]))
            {
                foreach (DataGridViewRow row2 in GridView.Rows)
                {
                    if (!row2.IsNewRow)
                        if ((int)row2.Cells["cyberteam_id"].Value == team_id)
                        {
                            row2.Cells["cyberteam_regdate"].Value = row.Cells["cyberteam_regdate"].Value;
                            row2.Cells["cyberteam_tier"].Value = row.Cells["cyberteam_tier"].Value;
                        }
                }
            }
                
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in GridView.SelectedRows)
                delete(row);
        }

        void delete(DataGridViewRow row)
        {
            if (row.IsNewRow)
            {
                MessageBox.Show("Пытаетесь удалить пустую строку", "Ошибка");
                return;
            }
            var connect = new NpgsqlConnection(connectstring);
            connect.Open();
            var command_delete = new NpgsqlCommand()
            {
                Connection = connect
            };
            command_delete.CommandText = "delete from player where player_id = @player_id";
            command_delete.Parameters.AddWithValue("@player_id", row.Cells["player_id"].Value);
            command_delete.ExecuteNonQuery();
            connect.Close();
            GridView.Rows.Remove(row);
        }

        private void GridView_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == 6)
            {
                var connect = new NpgsqlConnection(connectstring);
                connect.Open();
                var command_search_team = new NpgsqlCommand()
                {
                    Connection = connect
                };
                command_search_team.CommandText = "select cyberteam_id from team_name where cyberteam_name = @cyberteam_name";
                command_search_team.Parameters.AddWithValue("@cyberteam_name", GridView.Rows[e.RowIndex].Cells[e.ColumnIndex].Value);
                var reader = command_search_team.ExecuteReader();
                int? team_id;
                if (reader.Read())
                    team_id = (int?)reader[0];
                else
                    team_id = null;
                reader.Close();
                if (team_id != null)
                {
                    var command_select_team = new NpgsqlCommand()
                    {
                        Connection = connect
                    };
                    command_select_team.CommandText = "select cyberteam_regdate, cyberteam_tier from team_name where cyberteam_id = @cyberteam_id";
                    command_select_team.Parameters.AddWithValue("@cyberteam_id", team_id);
                    var reader2 = command_select_team.ExecuteReader();
                    reader2.Read();
                    GridView.Rows[e.RowIndex].Cells["cyberteam_regdate"].Value = reader2["cyberteam_regdate"];
                    GridView.Rows[e.RowIndex].Cells["cyberteam_tier"].Value = reader2["cyberteam_tier"];
                    GridView.Rows[e.RowIndex].Cells["cyberteam_regdate"].ReadOnly = true;
                    GridView.Rows[e.RowIndex].Cells["cyberteam_tier"].ReadOnly = true;
                }
                else
                {
                    GridView.Rows[e.RowIndex].Cells["cyberteam_regdate"].ReadOnly = false;
                    GridView.Rows[e.RowIndex].Cells["cyberteam_tier"].ReadOnly = false;
                }

                connect.Close();
            }
        }
    }


}
