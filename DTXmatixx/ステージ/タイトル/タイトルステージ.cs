﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SharpDX;
using SharpDX.Animation;
using SharpDX.DirectInput;
using SharpDX.Direct2D1;
using SharpDX.Direct2D1.Effects;
using FDK;
using FDK.メディア;
using DTXmatixx.アイキャッチ;

namespace DTXmatixx.ステージ.タイトル
{
	class タイトルステージ : ステージ
	{
		public enum フェーズ
		{
			表示,
			フェードアウト,
			確定,
			キャンセル,
		}
		public フェーズ 現在のフェーズ
		{
			get;
			protected set;
		}

		public タイトルステージ()
		{
			this.子リスト.Add( this._舞台画像 = new 舞台画像() );
			this.子リスト.Add( this._タイトルロゴ = new 画像( @"$(System)images\タイトルロゴ.png" ) );
			this.子リスト.Add( this._パッドを叩いてください = new 文字列画像() { 表示文字列 = "パッドを叩いてください", フォントサイズpt = 40f, 描画効果 = 文字列画像.効果.縁取り } );
		}

		protected override void On活性化( グラフィックデバイス gd )
		{
			using( Log.Block( FDKUtilities.現在のメソッド名 ) )
			{
				this._帯ブラシ = new SolidColorBrush( gd.D2DDeviceContext, new Color4( 0f, 0f, 0f, 0.8f ) );
				this.現在のフェーズ = フェーズ.表示;
			}
		}
		protected override void On非活性化( グラフィックデバイス gd )
		{
			using( Log.Block( FDKUtilities.現在のメソッド名 ) )
			{
				FDKUtilities.解放する( ref this._帯ブラシ );
			}
		}

		public override void 進行描画する( グラフィックデバイス gd )
		{
			App.入力管理.すべての入力デバイスをポーリングする();

			switch( this.現在のフェーズ )
			{
				case フェーズ.表示:

					this._舞台画像.進行描画する( gd );
					this._タイトルロゴ.描画する( gd, ( gd.設計画面サイズ.Width - this._タイトルロゴ.サイズ.Width ) / 2f, ( gd.設計画面サイズ.Height - this._タイトルロゴ.サイズ.Height ) / 2f - 100f );
					this._帯メッセージを描画する( gd );

					if( App.入力管理.シンバルが入力された() || App.入力管理.Keyboard.キーが押された( 0, Key.Return ) )
					{
						App.ステージ管理.アイキャッチを選択しクローズする( gd, nameof( シャッター ) );
						this.現在のフェーズ = フェーズ.フェードアウト;
					}
					else if( App.入力管理.ドラムが入力された( 入力.ドラム入力種別.Tom3 ) || App.入力管理.Keyboard.キーが押された( 0, Key.Escape ) )
					{
						this.現在のフェーズ = フェーズ.キャンセル;
					}
					break;

				case フェーズ.フェードアウト:

					this._舞台画像.進行描画する( gd );
					this._タイトルロゴ.描画する( gd, ( gd.設計画面サイズ.Width - this._タイトルロゴ.サイズ.Width ) / 2f, ( gd.設計画面サイズ.Height - this._タイトルロゴ.サイズ.Height ) / 2f - 100f );
					this._帯メッセージを描画する( gd );

					App.ステージ管理.現在のアイキャッチ.進行描画する( gd );

					if( App.ステージ管理.現在のアイキャッチ.現在のフェーズ == アイキャッチ.フェーズ.クローズ完了 )
					{
						this.現在のフェーズ = フェーズ.確定;
					}
					break;

				case フェーズ.確定:
				case フェーズ.キャンセル:
					break;
			}
		}

		private 舞台画像 _舞台画像 = null;
		private 画像 _タイトルロゴ = null;
		private Brush _帯ブラシ = null;
		private 文字列画像 _パッドを叩いてください = null;

		private void _帯メッセージを描画する( グラフィックデバイス gd )
		{
			var 領域 = new RectangleF( 0f, 800f, gd.設計画面サイズ.Width, 80f );

			gd.D2DBatchDraw( ( dc ) => {
				dc.FillRectangle( 領域, this._帯ブラシ );
			} );

			this._パッドを叩いてください.描画する( gd, 720f, 810f );
		}
	}
}
