using System;
using System.Linq;
using System.Collections.Generic;
using Verse;
using RimWorld;
using UnityEngine;
using System.Text;

namespace SaM {

	public class SaM_Mod : Verse.Mod {
		public static SaM_ModSettings settings;
		
		//
		// Constructors
		//
		public SaM_Mod(Verse.ModContentPack content) : base(content) {
			settings = this.GetSettings<SaM_ModSettings>();
			}
		
		//
		// Methods
		//
		public override String SettingsCategory() {
			return "SaM_Settings_category".Translate();
		}

		public override void DoSettingsWindowContents(Rect inRect)
		{
			Text.Font = GameFont.Small;

			float margin = 10;
			float xPos = inRect.x + margin;
			float yOff = inRect.y + margin;
			float posW = inRect.width - 2 * margin;

			Rect editCheckbox = new Rect(xPos, yOff, posW, 24);
			yOff += editCheckbox.height + 1;
			Rect editDescription = new Rect(xPos, yOff, posW, 24);

			Widgets.CheckboxLabeled(editCheckbox, "SaM_Settings_editOnBuild_label".Translate(), ref settings.editOnBuild, false);
			Widgets.Label(editDescription, "SaM_Settings_editOnBuild_description".Translate());
			
			yOff += editDescription.height + 9;

			Rect pauseCheckbox = new Rect(xPos, yOff, posW, 24);
			yOff += pauseCheckbox.height + 1;
			Rect pauseDescription = new Rect(xPos, yOff, posW, 24);
			
			Widgets.CheckboxLabeled(pauseCheckbox, "SaM_Settings_pauseOnEdit_label".Translate(), ref settings.pauseGameOnEdit, false);
			Widgets.Label(pauseDescription, "SaM_Settings_pauseOnEdit_description".Translate());
		}
	}

	public class SaM_ModSettings : Verse.ModSettings {
		//
		// Fields
		//
		public bool editOnBuild = false;
		public bool pauseGameOnEdit = false;

		//
		// Constructors
		//
		public SaM_ModSettings() {}

		//
		// Methods
		//
		public override void ExposeData() {
			Scribe_Values.Look<bool>(ref this.editOnBuild, "edit_on_build", false, false);
			Scribe_Values.Look<bool>(ref this.pauseGameOnEdit, "pause_game_on_edit", false, false);
		}
	}

	public class Building_Memorial_Base : Building {

        //
        // Static Fields
        //
        // this value is only correct when the object is placed.
		// in minified form this should be 5 but sadly we can't find that out easily
		// so there will be a stupid scrollbar while in minified form.
		// you can also not access the full text, but I believe that's not a bad thing
        //private static int MAX_LINES = 6;
        //private static int MAX_LENGTH = 408; // this is valid ONLY for GameFont.Small
		//private static GameFont TEXT_FONT = GameFont.Small;

		//
		// Fields
		//
		[Unsaved]
		private string text;

		//[ThisOneIsSavedTho]
		private string savedata;

		//
		// Properties
		//
		public string Text {
			get {
				return this.text;
			}
			set {
				if(this.text == value) {
					return;
				}
				this.text = value;
				SaveText();
			}
		}

		//
		// Constructors
		//
		public Building_Memorial_Base() {
		}

		//
		// Methods
		//
		public void SaveText() {
			try
			{
				Encoding encoding = Encoding.Unicode;
				savedata = Convert.ToBase64String(
					CompressUtility.Compress(encoding.GetBytes(text))
					);
			}
			catch(Exception E)
			{
				Log.ErrorOnce("Unable to encode memorial data for " + this.ThingID + ": " + E.ToString(), this.ThingID.GetHashCode());
			}
		}
		private void LoadText() {
			try
			{
				Encoding encoding = Encoding.Unicode;
				char[] base64 = encoding.GetChars(
					CompressUtility.Decompress( Convert.FromBase64String(savedata) )
					);
				text = new string(base64);
			}
			catch(Exception E)
			{
				Log.ErrorOnce("Unable to decode memorial data for " + this.ThingID + ": " + E.ToString(), this.ThingID.GetHashCode());
			}
		}

		public override void ExposeData() {
			base.ExposeData();
			SaveText();
			Scribe_Values.Look(ref savedata, "text");
		}

		public override void PostMapInit() {
			base.PostMapInit();
			LoadText();
		}

		// THIS METHOD HANDLES THE STRING IN THE BOTTOM LEFT
		public override string GetInspectString() {
			/*
			// REQUIRED FOR Text.CalcSize TO WORK PROPERLY
			Text.Font = TEXT_FONT;

			List<string> lines = new List<string>();

			foreach(string line in this.text.Split('\n')) {
				string short_line = "";
                if (string.IsNullOrEmpty(line))
                {
                    // lazy fix for an error that occurs when the lower display contains empty lines.
                    // usually that error is most likely valid, but here I believe it's best
                    // to preserve those empty lines to allow for greater stylistic freedom.
                    lines.Add(" ");
					// another valid alternative would be to skip those empty lines in the small summary,
					// but I feel it's better to show the "correct" representation of the text in question
                    continue;
                }
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
				lines = lines.GetRange(0, MAX_LINES - 1);
				lines.Add("(...)");
			}
			return String.Join("\n", lines.ToArray());
			*/
			return this.text;
		}

		public override TipSignal GetTooltip() {
			return new TipSignal(this.text, this.thingIDNumber * 152317 /*251235*/, TooltipPriority.Default);
		}

		public override void SpawnSetup(Map map, bool respawningAfterLoad) {
			base.SpawnSetup(map, respawningAfterLoad);
			if(SaM_Mod.settings?.editOnBuild ?? false) {
				CameraJumper.TryJumpAndSelect(this);
				Find.MainTabsRoot.SetCurrentTab(MainButtonDefOf.Inspect);
				((MainTabWindow_Inspect)MainButtonDefOf.Inspect.TabWindow).OpenTabType = this.GetInspectTabs().OfType<ITab_View>().First().GetType();
			}
		}
	}
		
	public class ITab_View : ITab {

		//
		// Fields
		//
		private bool editing = false;
		private string text = "";
		private string tabThingID = null;

		[Unsaved]
		private bool already_paused = false;

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
		public override void TabUpdate() {
			base.TabUpdate();
			if(this.tabThingID != base.SelThing.GetUniqueLoadID()) {
				this.text = ((Building_Memorial_Base)SelThing).Text;
				this.tabThingID = base.SelThing.GetUniqueLoadID();
			}
		}

		protected override void FillTab() {
			Text.Font = GameFont.Small;

			Rect rectText = new Rect(20, 20, this.size.x - 40, this.size.y - 75);
			Rect rectButton = new Rect(20, this.size.y - 50, this.size.x - 40, 30);
			Rect rectCancel = new Rect(rectButton.x, rectButton.y, (rectButton.width - 10) / 2, rectButton.height);
			Rect rectSave = new Rect((this.size.x + 10) / 2, rectButton.y, (rectButton.width - 10) / 2, rectButton.height);

			if(this.editing) {
				Building_Memorial_Base memorial = (Building_Memorial_Base)base.SelThing;

				this.text = Widgets.TextArea(rectText, this.text);

				if(Widgets.ButtonText(rectCancel, "SaM_TabView_Cancel".Translate())) {
					this.editing = false;
					this.text = memorial.Text;
				}
				if(Widgets.ButtonText(rectSave, "SaM_TabView_Save".Translate())) {
					this.editing = false;
					memorial.Text = this.text;
				}
				if(!editing) { //state changed
					if(SaM_Mod.settings.pauseGameOnEdit && Find.TickManager.Paused && !already_paused) {
						Find.TickManager.TogglePaused();
					}
					memorial.SaveText();
				}
			} else {
				Widgets.Label(rectText, this.text);

				if(Widgets.ButtonText(rectButton, "SaM_TabView_Edit".Translate())) {
					this.editing = true;
					if(SaM_Mod.settings.pauseGameOnEdit) {
						if(Find.TickManager.Paused) already_paused = true;
						else {
							already_paused = false;
							Find.TickManager.TogglePaused();
						}
					}
				}
			}
		}
	}
}

