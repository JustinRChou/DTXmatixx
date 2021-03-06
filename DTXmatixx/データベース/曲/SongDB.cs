﻿using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Diagnostics;
using System.IO;
using System.Linq;
using FDK;
using SSTFormatCurrent = SSTFormat.v3;

namespace DTXmatixx.データベース.曲
{
	using Song01 = old.Song01;
	using Song02 = old.Song02;
	using Song = Song03;	// 最新バージョンを指定(1/2)。

	/// <summary>
	///		曲データベースに対応するエンティティクラス。
	/// </summary>
	class SongDB : SQLiteDBBase
	{
		public const long VERSION = 3;	// 最新バージョンを指定(2/2)。

		public Table<Song> Songs
			=> base.DataContext.GetTable<Song>();

		public SongDB()
			: base(  @"$(AppData)SongDB.sqlite3", VERSION )
		{
		}

		protected override void テーブルがなければ作成する()
		{
			using( var transaction = this.Connection.BeginTransaction() )
			{
				try
				{
					// 最新のバージョンのテーブルを作成する。
					this.DataContext.ExecuteCommand( $"CREATE TABLE IF NOT EXISTS Songs {Song.ColumnsList};" );
					this.DataContext.SubmitChanges();

					// 成功。
					transaction.Commit();
				}
				catch
				{
					// 失敗。
					transaction.Rollback();
				}
			}
		}
		protected override void データベースのアップグレードマイグレーションを行う( long 移行元DBバージョン )
		{
			switch( 移行元DBバージョン )
			{
				case 2:
					#region " 2 → 3 "
					//----------------
					this.DataContext.ExecuteCommand( "PRAGMA foreign_keys = OFF" );
					this.DataContext.SubmitChanges();
					using( var transaction = this.Connection.BeginTransaction() )
					{
						try
						{
							// テータベースをアップデートしてデータを移行する。
							this.DataContext.ExecuteCommand( $"CREATE TABLE new_Songs {Song03.ColumnsList}" );
							this.DataContext.ExecuteCommand( $"INSERT INTO new_Songs SELECT *,null FROM Songs" );   // 追加されたカラムは null
							this.DataContext.ExecuteCommand( $"DROP TABLE Songs" );
							this.DataContext.ExecuteCommand( $"ALTER TABLE new_Songs RENAME TO Songs" );
							this.DataContext.ExecuteCommand( "PRAGMA foreign_keys = ON" );
							this.DataContext.SubmitChanges();

							// すべてのレコードについて、追加されたカラムを更新する。
							var song03s = base.DataContext.GetTable<Song03>();
							foreach( var song03 in song03s )
							{
								var score = (SSTFormatCurrent.スコア) null;

								#region " スコアを読み込む "
								//----------------
								var 拡張子名 = Path.GetExtension( song03.Path );
								if( ".sstf" == 拡張子名 )
								{
									score = new SSTFormatCurrent.スコア( song03.Path );
								}
								else if( ".dtx" == 拡張子名 )
								{
									score = SSTFormatCurrent.DTXReader.ReadFromFile( song03.Path );
								}
								else
								{
									throw new Exception( $"未対応のフォーマットファイルです。[{song03.Path.ToVariablePath().変数付きパス}]" );
								}
								//----------------
								#endregion

								using( score )
								{
									// Artist カラムの更新。
									if( score.アーティスト名.Nullでも空でもない() )
									{
										song03.Artist = score.アーティスト名;
									}
									else
									{
										song03.Artist = ""; // null不可
									}
								}
							}
							this.DataContext.SubmitChanges();

							// 成功。
							transaction.Commit();
							Log.Info( "Songs テーブルをアップデートしました。[2→3]" );
						}
						catch
						{
							// 失敗。
							transaction.Rollback();
							Log.ERROR( "Songs テーブルのアップデートに失敗しました。[2→3]" );
						}
					}
					//----------------
					#endregion
					break;

				case 1:
					#region " 1 → 2 "
					//----------------
					this.DataContext.ExecuteCommand( "PRAGMA foreign_keys = OFF" );
					this.DataContext.SubmitChanges();
					using( var transaction = this.Connection.BeginTransaction() )
					{
						try
						{
							// テータベースをアップデートしてデータを移行する。
							this.DataContext.ExecuteCommand( $"CREATE TABLE new_Songs {Song02.ColumnsList}" );
							this.DataContext.ExecuteCommand( $"INSERT INTO new_Songs SELECT *,null FROM Songs" );   // 追加されたカラムは null
							this.DataContext.ExecuteCommand( $"DROP TABLE Songs" );
							this.DataContext.ExecuteCommand( $"ALTER TABLE new_Songs RENAME TO Songs" );
							this.DataContext.ExecuteCommand( "PRAGMA foreign_keys = ON" );
							this.DataContext.SubmitChanges();

							// すべてのレコードについて、追加されたカラムを更新する。
							var song02s = base.DataContext.GetTable<Song02>();
							foreach( var song02 in song02s )
							{
								var 拡張子名 = Path.GetExtension( song02.Path );
								var score = (SSTFormatCurrent.スコア) null;

								#region " スコアを読み込む "
								//----------------
								if( ".sstf" == 拡張子名 )
								{
									score = new SSTFormatCurrent.スコア( song02.Path );
								}
								else if( ".dtx" == 拡張子名 )
								{
									score = SSTFormatCurrent.DTXReader.ReadFromFile( song02.Path );
								}
								else
								{
									throw new Exception( $"未対応のフォーマットファイルです。[{song02.Path.ToVariablePath().変数付きパス}]" );
								}
								//----------------
								#endregion

								using( score )
								{
									// PreImage カラムの更新。
									if( score.プレビュー画像.Nullでも空でもない() )
									{
										// プレビュー画像は、曲ファイルからの相対パス。
										song02.PreImage = Path.Combine( Path.GetDirectoryName( song02.Path ), score.プレビュー画像 );
									}
									else
									{
										song02.PreImage =
											( from ファイル名 in Directory.GetFiles( Path.GetDirectoryName( song02.Path ) )
											  where _対応するサムネイル画像名.Any( thumbファイル名 => ( Path.GetFileName( ファイル名 ).ToLower() == thumbファイル名 ) )
											  select ファイル名 ).FirstOrDefault();
									}
								}
							}
							this.DataContext.SubmitChanges();

							// 成功。
							transaction.Commit();
							Log.Info( "Songs テーブルをアップデートしました。[1→2]" );
						}
						catch
						{
							// 失敗。
							transaction.Rollback();
							Log.ERROR( "Songs テーブルのアップデートに失敗しました。[1→2]" );
						}
					}
					//----------------
					#endregion
					break;

				default:
					throw new Exception( $"移行元DBのバージョン({移行元DBバージョン})がマイグレーションに未対応です。" );
			}
		}

		private readonly string[] _対応するサムネイル画像名 = { "thumb.png", "thumb.bmp", "thumb.jpg", "thumb.jpeg" };
	}
}
