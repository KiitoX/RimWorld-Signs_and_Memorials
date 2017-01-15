using System;
using System.Linq;
using System.Collections.Generic;
using Verse;
using RimWorld;
using UnityEngine;

namespace SaM {

	public class Base : Building {
		
		//
		// Static Fields
		//
		private static int MAX_LINES = 6;
		private static int MAX_LENGTH = 408; // valid for GameFont.Small

		//
		// Fields
		//
		public string text;

		//
		// Constructors
		//
		public Base() {
			this.text = "SaM_Placeholder".Translate();
		}

		//
		// Methods
		//
		// THIS METHOD HANDLES THE STRING IN THE BOTTOM RIGHT
		public override string GetInspectString() {

			// REQUIRED FOR Text.CalcSize TO WORK PROPERLY
			Text.Font = GameFont.Small;

			List<string> lines = new List<string>();

			foreach(string line in this.text.Split('\n')) {
				string short_line = "";
				if (Text.CalcSize(line).x > MAX_LENGTH) {
					foreach(string word in line.Split(' ')) {
						if(Text.CalcSize(short_line + word).x < MAX_LENGTH) {
							short_line += word + ' ';
						} else if(Text.CalcSize(short_line + word).x > MAX_LENGTH) {
							if(short_line.Length > 0) {
								lines.Add(short_line);
								short_line = "";
							}
							Stack<char> _word = new Stack<char>(word.Reverse());
							while(Text.CalcSize(new String(_word.ToArray())).x >= MAX_LENGTH) {
								string short_word = "";
								while(Text.CalcSize(short_word + _word.Peek()).x < MAX_LENGTH) {
									short_word += _word.Pop();
								}
								lines.Add(short_word);
							}
							short_line = new String(_word.ToArray()) + " ";
						} else {
							lines.Add(short_line + word);
							short_line = "";
						}
					}
				} else {
					short_line = line;
				}
				lines.Add(short_line.Trim());
			}

			if(lines.Count > MAX_LINES) {
				lines = lines.GetRange(0, MAX_LINES-1);
				lines.Add("(...)");
			}
			return String.Join("\n", lines.ToArray());
		}

	}

	public class ITab_View : ITab {
		
		//
		// Fields
		//
		public bool editing;
		public string text;

		//
		// Properties
		//
		public override bool IsVisible {
			get {
				return true;
			}
		}
		
		//
		// Constructors
		//
		public ITab_View() {
			this.size = new Vector2(570, 470);
			this.labelKey = "SaM_TabView";
			this.tutorTag = "View";
		}

		//
		// Methods
		//
		public override void OnOpen() {
			this.text = ((Base)base.SelThing).text;
		}

		protected override void FillTab() {
			Text.Font = GameFont.Small;

			Rect text = new Rect(20, 20, this.size.x - 40, this.size.y - 75);
			Rect button = new Rect(20, this.size.y - 50, this.size.x - 40, 30);

			if(this.editing) {
				this.text = Widgets.TextArea(text, this.text);

				if(Widgets.ButtonText(new Rect(button.x, button.y, (button.width - 10) / 2, button.height), "SaM_TabView_Cancel".Translate())) {
					this.editing = false;
					this.text = ((Base)base.SelThing).text;
				}
				if(Widgets.ButtonText(new Rect((this.size.x + 10) / 2, button.y, (button.width - 10) / 2, button.height), "SaM_TabView_Save".Translate())) {
					this.editing = false;
					((Base)base.SelThing).text = this.text;
				}
			} else {
				Widgets.Label(text, this.text);

				if(Widgets.ButtonText(button, "SaM_TabView_Edit".Translate())) {
					this.editing = true;
				}
			}
		}
	}
}

