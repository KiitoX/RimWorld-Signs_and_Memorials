using System;
using System.Linq;
using System.Collections.Generic;
using Verse;
using RimWorld;
using UnityEngine;

namespace SaM {

	public class SaM_Mod : Verse.Mod {

		//
		// Constructors
		//
		public SaM_Mod(Verse.ModContentPack content) : base(content) {}

		//
		// Methods
		//
		public override String SettingsCategory() {
			return "SaM_Settings_category".Translate();
		}

		public override void DoSettingsWindowContents(Rect inRect) {
			SaM_ModSettings settings = this.GetSettings<SaM_ModSettings>();
			Text.Font = GameFont.Small;

			float margin = 10;
			float xPos = inRect.x + margin;
			float yOff = inRect.y + margin;
			float posW = inRect.width - 2 * margin;

			Rect editCheckbox = new Rect(xPos, yOff, posW, 20);
			yOff += editCheckbox.height;
			Rect editDescription = new Rect(xPos, yOff, posW, 20);

			Widgets.CheckboxLabeled(editCheckbox, "SaM_Settings_editOnBuild_label".Translate(), ref settings.editOnBuild, false);
			Widgets.Label(editDescription, "SaM_Settings_editOnBuild_description".Translate());

			// It seems to already work as is, but supposedly this has to be called to save the settings.
			// Perhaps this only applies if the settings are changed from an 'external' context...
			// LoadedModManager.GetMod<SaM_Mod>().WriteSettings();
		}
	}

	public class SaM_ModSettings : Verse.ModSettings {

		/* TODO:
		 *
		 * Pause game on edit, would require some looking around, probably...
		 */

		//
		// Fields
		//
		public bool editOnBuild;

		//
		// Constructors
		//
		public SaM_ModSettings() {}

		//
		// Methods
		//
		public override void ExposeData() {
			Scribe_Values.Look<bool>(ref this.editOnBuild, "edit_on_build", false, false);
		}
	}

	public class Base : Building {

		//
		// Static Fields
		//
		private static int MAX_LINES = 6;
		private static int MAX_LENGTH = 408; // valid ONLY for GameFont.Small
		private static GameFont TEXT_FONT = GameFont.Small;

		//
		// Fields
		//
		private SaM_ModSettings settings;

		private CompText textComp;

		//
		// Properties
		//
		public string text {
			get {
				return this.textComp.text;
			}
			set {
				if(this.textComp.text == value) {
					return;
				}
				this.textComp.text = value;
			}
		}

		//
		// Constructors
		//
		public Base() {
			this.settings = LoadedModManager.GetMod<SaM_Mod>().GetSettings<SaM_ModSettings>();
		}

		//
		// Methods
		//
		public override void ExposeData() {
			base.ExposeData();
			if(Scribe.mode == LoadSaveMode.PostLoadInit) {
				if(this.textComp == null) {
					this.textComp = base.GetComp<CompText>();
				}
			}
		}

		// THIS METHOD HANDLES THE STRING IN THE BOTTOM LEFT
		public override string GetInspectString() {

			// REQUIRED FOR Text.CalcSize TO WORK PROPERLY
			Text.Font = TEXT_FONT;

			List<string> lines = new List<string>();

			foreach(string line in this.text.Split('\n')) {
				string short_line = "";
				if(Text.CalcSize(line).x > MAX_LENGTH) {
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
		}

		public override TipSignal GetTooltip() {
			return new TipSignal(this.text, this.thingIDNumber * 152317 /*251235*/, TooltipPriority.Default);
		}

		public override void SpawnSetup(Map map, bool respawningAfterLoad) {
			base.SpawnSetup(map, respawningAfterLoad);
			this.textComp = base.GetComp<CompText>();
			if(this.settings.editOnBuild) {
				CameraJumper.TryJumpAndSelect(this);
				Find.MainTabsRoot.SetCurrentTab(MainButtonDefOf.Inspect);
				((MainTabWindow_Inspect)MainButtonDefOf.Inspect.TabWindow).OpenTabType = this.GetInspectTabs().OfType<ITab_View>().First().GetType();
			}
		}

	}

	public class CompText : ThingComp {
		//
		// Fields
		//
		public string text = "SaM_Placeholder".Translate();

		//
		// Methods
		//
		public override void PostExposeData() {
			base.PostExposeData();
			Scribe_Values.Look<string>(ref this.text, "text", "ERROR LOADING", false);
		}
	}

	public class CompProperties_Text : CompProperties {
		//
		// Constructors
		//
		public CompProperties_Text() {
			this.compClass = typeof(CompText);
		}
	}

	public class ITab_View : ITab {

		//
		// Fields
		//
		private bool editing = false;
		private string text = "";
		private string tabThingID = null;

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
				this.text = ((Base)base.SelThing).text;
				this.tabThingID = base.SelThing.GetUniqueLoadID();
			}
		}

		protected override void FillTab() {
			Text.Font = GameFont.Small;

			Rect text = new Rect(20, 20, this.size.x - 40, this.size.y - 75);
			Rect button = new Rect(20, this.size.y - 50, this.size.x - 40, 30);
			Rect cancel = new Rect(button.x, button.y, (button.width - 10) / 2, button.height);
			Rect save = new Rect((this.size.x + 10) / 2, button.y, (button.width - 10) / 2, button.height);

			if(this.editing) {
				this.text = Widgets.TextArea(text, this.text);

				if(Widgets.ButtonText(cancel, "SaM_TabView_Cancel".Translate())) {
					this.editing = false;
					this.text = ((Base)base.SelThing).text;
				}
				if(Widgets.ButtonText(save, "SaM_TabView_Save".Translate())) {
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

