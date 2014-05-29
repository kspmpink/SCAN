/* 
 * Scientific Committee on Advanced Navigation S.C.A.N. Satellite
 * SCANsat - User Interface
 * 
 * Copyright (c)2013 damny; see LICENSE.txt for licensing details.
 */

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace SCANsat
{
	public class SCANui
	{
		private static string infotext = null, title = "S.C.A.N. Planetary Mapping";
		private static Rect pos_infobox = new Rect(10f, 55f, 10f, 10f);
		private static Rect pos_bigmap = new Rect(100f, 100f, 10f, 10f);
		private static Rect pos_spotmap = new Rect(10f, 10f, 10f, 10f), pos_spotmap_x = new Rect(10f, 10f, 25f, 25f);
		private static Rect pos_instruments = new Rect(-1, 100f, 10f, 10f);
		private static Rect pos_settings = new Rect(-1, 100f, 0f, 10f);
		private static Rect rc = new Rect(0, 0, 0, 0);
		private static bool bigmap_dragging, icon_dragging;
		private static float bigmap_drag_w, bigmap_drag_x;
		private static SCANmap bigmap, spotmap;
		private static Texture2D overlay_static, test_image;
		private static bool overlay_static_dirty;
		private static bool bigmap_visible, instruments_visible, settings_visible;
		private static bool gui_active, notMappingToday;
		private static int maptraq_frame;
		private static int gui_frame_ping, gui_frame_draw;
		private static Callback guicb = new Callback(gui_show);
		private static GUIStyle style_label = new GUIStyle();
		private static GUIStyle style_headline;
		private static GUIStyle style_readout;
		private static GUIStyle style_button;
		private static GUIStyle style_toggle;
		private static GUIStyle style_overlay;
		private static Font dotty;

		// colourblind barrier-free colours, according to Masataka Okabe and Kei Ito
		// http://jfly.iam.u-tokyo.ac.jp/color/
		public static Color cb_orange = new Color(0.9f, 0.6f, 0f);
		// orange
		public static Color cb_skyBlue = new Color(0.35f, 0.7f, 0.9f);
		// sky blue
		public static Color cb_bluishGreen = new Color(0f, 0.6f, 0.5f);
		// bluish green
		public static Color cb_yellow = new Color(0.95f, 0.9f, 0.25f);
		// yellow
		public static Color cb_blue = new Color(0f, 0.45f, 0.7f);
		// blue
		public static Color cb_vermillion = new Color(0.8f, 0.4f, 0f);
		// vermillion
		public static Color cb_reddishPurple = new Color(0.8f, 0.6f, 0.7f);
		// reddish purple
		public static Color c_good {
			get { 
				if(SCANcontroller.controller.colours != 1) return XKCDColors.PukeGreen;
				return cb_skyBlue;
			}
		}

		public static Color c_bad = cb_orange;

		public static Color c_ugly {
			get {
				if(SCANcontroller.controller.colours != 1) return XKCDColors.LightRed;
				return cb_yellow;
			}
		}

		public static string colorHex(Color32 c) {
			return "#" + c.r.ToString("x2") + c.g.ToString("x2") + c.b.ToString("x2");
		}

		public static string colored(Color c, string text) {
			text = "<color=\"" + colorHex(c) + "\">" + text + "</color>";
			return text;
		}
		// icons available from MapView as of 0.22
		public enum OrbitIcon : int
		{
			Pe = 0,
			Ap = 1,
			AN = 2,
			DN = 3,
			Ship = 5,
			Debris = 6,
			Planet = 7,
			Mystery = 8,
			Encounter = 10,
			Exit = 11,
			EVA = 12,
			Ball = 13,
			TargetTop = 15,
			TargetBottom = 16,
			ManeuverNode = 17,
			Station = 18,
			Rover = 20,
			Probe = 21,
			Base = 22,
			Lander = 23,
			Flag = 24,
		}

		private static Rect pos_icon = new Rect(0, 0, 0, 0);
		private static Rect tc_icon = new Rect(0, 0, 0, 0);

		public static void drawOrbitIcon(int x, int y, OrbitIcon icon, Color col, int width, bool outline) {
			pos_icon.x = x - width / 2;
			pos_icon.y = y - width / 2;
			pos_icon.width = width;
			pos_icon.height = width;
			tc_icon.width = 0.2f;
			tc_icon.height = 0.2f;
			tc_icon.x = 0.2f * ((int)icon % 5);
			tc_icon.y = 0.2f * (4 - (int)icon / 5);
			Color cold = GUI.color;
			if(outline) {
				GUI.color = Color.black;
				pos_icon.x -= 1;
				GUI.DrawTextureWithTexCoords(pos_icon, MapView.OrbitIconsMap, tc_icon);
				pos_icon.x += 2;
				GUI.DrawTextureWithTexCoords(pos_icon, MapView.OrbitIconsMap, tc_icon);
				pos_icon.x -= 1;
				pos_icon.y -= 1;
				GUI.DrawTextureWithTexCoords(pos_icon, MapView.OrbitIconsMap, tc_icon);
				pos_icon.y += 2;
				GUI.DrawTextureWithTexCoords(pos_icon, MapView.OrbitIconsMap, tc_icon);
				pos_icon.y -= 1;
			}
			GUI.color = col;
			GUI.DrawTextureWithTexCoords(pos_icon, MapView.OrbitIconsMap, tc_icon);
			GUI.color = cold;
		}

		public static OrbitIcon orbitIconForVesselType(VesselType type) {
			switch(type) {
			case VesselType.Base:
				return OrbitIcon.Base;
			case VesselType.Debris:
				return OrbitIcon.Debris;
			case VesselType.EVA:
				return OrbitIcon.EVA;
			case VesselType.Flag:
				return OrbitIcon.Flag;
			case VesselType.Lander:
				return OrbitIcon.Lander;
			case VesselType.Probe:
				return OrbitIcon.Probe;
			case VesselType.Rover:
				return OrbitIcon.Rover;
			case VesselType.Ship:
				return OrbitIcon.Ship;
			case VesselType.Station:
				return OrbitIcon.Station;
			case VesselType.Unknown:
				return OrbitIcon.Mystery;
			default:
				return OrbitIcon.Mystery;
			}
		}

		private static void drawLabel(Rect r, Color c, string txt, bool aligned, bool outline) {
			if(txt.Length < 1) return;
			style_label.normal.textColor = Color.black;
			if(aligned) {
				Vector2 sz = style_label.CalcSize(new GUIContent(txt.Substring(0, 1)));
				r.x -= sz.x / 2;
				r.y -= sz.y / 2;
			}
			if(outline) {
				r.x -= 1;
				GUI.Label(r, txt, style_label);
				r.x += 2;
				GUI.Label(r, txt, style_label);
				r.x -= 1; 
				r.y -= 1;
				GUI.Label(r, txt, style_label);
				r.y += 2;
				GUI.Label(r, txt, style_label);
				r.y -= 1;
			}
			style_label.normal.textColor = c;
			GUI.Label(r, txt, style_label);
		}

		private static void drawVesselLabel(Rect maprect, SCANmap map, int num, Vessel vessel) {
			double lon = (vessel.longitude + 360 + 180) % 360;
			double lat = (vessel.latitude + 180 + 90) % 180;
			if(map != null) {
				lat = (map.projectLatitude(vessel.longitude, vessel.latitude) + 90) % 180;
				lon = (map.projectLongitude(vessel.longitude, vessel.latitude) + 180) % 360;
				lat = map.scaleLatitude(lat);
				lon = map.scaleLongitude(lon);
				if(lat < 0 || lon < 0 || lat > 180 || lon > 360) return;
			}
			lon = lon * maprect.width / 360f;
			lat = maprect.height - lat * maprect.height / 180f;
			string txt = num.ToString();
			if(num == 0) txt = vessel.vesselName;
			else if(num < 0) txt = "";
			Rect r = new Rect(maprect.x + (float)lon, maprect.y + (float)lat, 250f, 25f);
			Color col = Color.white;
			if(SCANcontroller.controller.colours == 1 && vessel != FlightGlobals.ActiveVessel) col = cb_skyBlue;
			if(vessel == FlightGlobals.ActiveVessel && (int)(Time.realtimeSinceStartup % 2) == 0) {
				col = cb_yellow;
			}
			int sz = 16;
			if(vessel.vesselType == VesselType.Flag) sz = 24;
			drawOrbitIcon((int)r.x, (int)r.y, orbitIconForVesselType(vessel.vesselType), col, sz, true);
			if(maprect.width < 360) return;
			r.x += 12;
			drawLabel(r, col, txt, true, true);
		}

		private static void drawAnomalyLabel(Rect maprect, SCANmap map, SCANdata.SCANanomaly anomaly) {
			if(!anomaly.known) return;
			double lon = (anomaly.longitude + 360 + 180) % 360;
			double lat = (anomaly.latitude + 180 + 90) % 180;
			if(map != null) {
				lat = (map.projectLatitude(anomaly.longitude, anomaly.latitude) + 90) % 180;
				lon = (map.projectLongitude(anomaly.longitude, anomaly.latitude) + 180) % 360;
				lat = map.scaleLatitude(lat);
				lon = map.scaleLongitude(lon);
				if(lat < 0 || lon < 0 || lat > 180 || lon > 360) return;
			} 
			lon = lon * maprect.width / 360f;
			lat = maprect.height - lat * maprect.height / 180f;
			string txt = SCANcontroller.controller.anomalyMarker + " " + anomaly.name;
			if(!anomaly.detail) txt = SCANcontroller.controller.anomalyMarker + " Anomaly";
			Rect r = new Rect(maprect.x + (float)lon, maprect.y + (float)lat, 250f, 25f);
			drawLabel(r, cb_yellow, txt, true, true);
		}

		private static bool readableLabel(string text, Color c) {
			string textpoor = Regex.Replace(text, "<[^>]*>", "");
			GUI.color = Color.black;
			style_label.normal.textColor = Color.black;
			GUILayout.Label(textpoor, style_label);
			Rect r = GUILayoutUtility.GetLastRect();
			r.x += 2;
			GUI.Label(r, textpoor, style_label);
			r.x -= 1;
			r.y -= 1;
			GUI.Label(r, textpoor, style_label);
			r.y += 2;
			GUI.Label(r, textpoor, style_label);
			r.y -= 1;
			style_label.normal.textColor = c;
			GUI.color = Color.white;
			GUI.Label(r, text, style_label);
			if(!Event.current.isMouse) return false;
			return r.Contains(Event.current.mousePosition);
		}

		private static string toDMS(double thing, string neg, string pos) {
			string dms = "";
			if(thing >= 0) neg = pos;
			thing = Math.Abs(thing);
			dms += Math.Floor(thing).ToString() + "°";
			thing = (thing - Math.Floor(thing)) * 60;
			dms += Math.Floor(thing).ToString() + "'";
			thing = (thing - Math.Floor(thing)) * 60;
			dms += thing.ToString("F2") + "\"";
			dms += neg;
			return dms;
		}

		private static string toDMS(double lat, double lon) {
			return toDMS(lat, "S", "N") + " " + toDMS(lon, "W", "E");
		}

		public static Texture2D getTestImage() {
			if(test_image != null) return test_image;
			test_image = new Texture2D(360, 180, TextureFormat.RGB24, false);
			Color[] pix = test_image.GetPixels();

			int pos = 0;
			for(int y=0; y<180; ++y) {
				for(int x=0; x<360; ++x) {
					Color c = Color.black;
					if(y < 120) {
						switch((int)(x / 51)) {
						case 0:
							c = Color.grey;
							break;
						case 1:
							c = Color.yellow;
							break;
						case 2:
							c = Color.cyan;
							break;
						case 3:
							c = Color.green;
							break;
						case 4:
							c = Color.magenta;
							break;
						case 5:
							c = Color.red;
							break;
						default:
							c = Color.blue;
							break;
						}
					} else if(y < 150) {
						switch((int)(x / 51)) {
						case 0:
							c = Color.blue;
							break;
						case 1:
							c = Color.black;
							break;
						case 2:
							c = Color.magenta;
							break;
						case 3:
							c = Color.black;
							break;
						case 4:
							c = Color.cyan;
							break;
						case 5:
							c = Color.black;
							break;
						default:
							c = Color.grey;
							break;
						}
					} else {
						switch((int)(x / 60)) {
						case 0:
							c = XKCDColors.DarkTeal;
							break;
						case 1:
							c = Color.white;
							break;
						case 2:
							c = XKCDColors.DarkPurple;
							break;
						case 3:
							c = XKCDColors.DarkGrey;
							break;
						case 4:
							c = Color.grey;
							break;
						default:
							c = Color.black;
							break;
						}

					}
					pix[pos++] = c;
				}
			}

			test_image.SetPixels(pix);
			test_image.Apply();
			return test_image;
		}

		private static int minimode = 2, lastmode = -1;
		private static Color minicolor = Color.white;

		private static void gui_infobox_build(int wid) {
			GUI.skin = null;
			bool repainting = Event.current.type == EventType.Repaint;
			Vessel vessel = FlightGlobals.ActiveVessel;
			SCANdata data = SCANcontroller.controller.getData(vessel.mainBody);
			Rect r;

			if(minimode > 0) {
				GUILayout.BeginVertical();

				if(notMappingToday) {
					GUILayout.Label(getTestImage());
				} else {
					GUILayout.Label(data.map_small);
				}
				Rect maprect = GUILayoutUtility.GetLastRect();
				maprect.width = data.map_small.width;
				maprect.height = data.map_small.height;

				if(infotext != null) {
					GUILayout.BeginVertical();
					readableLabel(infotext, Color.white);
					GUILayout.EndVertical();
				}

				GUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
				if(GUILayout.Button("Big Map")) {
					bigmap_visible = !bigmap_visible;
					if(!bigmap_visible) spotmap = null;
				}
				if(GUILayout.Button("Instruments")) {
					instruments_visible = !instruments_visible;
				}
				if(GUILayout.Button("Settings")) {
					settings_visible = !settings_visible;
				}
				GUILayout.EndHorizontal();

				if(!notMappingToday) {
					int count = 1;
					foreach(Vessel v in FlightGlobals.Vessels) {
						if(v == null) continue;
						if(SCANcontroller.controller.isVesselKnown(v) || v == FlightGlobals.ActiveVessel) {
							if(v.mainBody == vessel.mainBody) {
								float lon = (float)(v.longitude + 360 + 180) % 360 - 180;
								float lat = (float)(v.latitude + 180 + 90) % 180 - 90;
								if(minimode == 1) {
									drawVesselLabel(maprect, null, -1, v);
									continue;
								}
								float alt = v.heightFromTerrain;
								if(alt < 0) alt = (float)v.altitude;
								string text = "[" + count.ToString() + "] <b>" + v.vesselName + "</b> (" + lat.ToString("F1") + "," + lon.ToString("F1") + "; " + alt.ToString("N1") + "m)";
								Color col = XKCDColors.LightGrey;
								if(v == FlightGlobals.ActiveVessel) col = c_good;
								GUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
								if(readableLabel(text, col)) {
									if(Event.current.clickCount > 1) {
										Event.current.Use();
										FlightGlobals.SetActiveVessel(v);
										ScreenMessages.PostScreenMessage(v.vesselName, 5, ScreenMessageStyle.UPPER_CENTER);
									}
								}
								GUILayout.EndHorizontal();
								drawVesselLabel(maprect, null, count, v);
								count += 1;
							}
						}
					}
				}
				GUILayout.EndVertical();
				r = new Rect(pos_infobox.width - 50, 0, 22, 22);
				style_button.normal.textColor = cb_yellow;
				if(minimode == 2) {
					if(GUI.Button(r, "-", style_button)) minimode = 1;
				} else {
					if(GUI.Button(r, "+", style_button)) minimode = 2;
				}
				r.x += 25;
				style_button.normal.textColor = cb_vermillion;
				if(GUI.Button(r, SCANcontroller.controller.closeBox, style_button)) minimode = (minimode == 0 ? 2 : -minimode);
			} else {
				GUILayout.Label("", GUILayout.Width(32), GUILayout.Height(32));
				r = GUILayoutUtility.GetLastRect();
				drawOrbitIcon((int)(r.x + r.width / 2), (int)(r.y + r.height / 2), OrbitIcon.Probe, minicolor, 32, true);
				if(Event.current.isMouse) {
					if(Event.current.type == EventType.MouseUp) {
						if(icon_dragging) {
							icon_dragging = false;
						} else {
							if(r.Contains(Event.current.mousePosition)) {
								minimode = (minimode == 0 ? 2 : -minimode);
							}
						}
					} else if(Event.current.type == EventType.MouseDrag) {
						icon_dragging = true;
					}
				}
			}

			GUI.DragWindow();
		}

		private static void snapWindow(ref Rect r, Rect old) {
			int dist = 32;
			bool left = old.x < dist;
			bool right = (old.x + old.width) > Screen.width - dist;
			bool top = old.y < dist;
			bool bottom = (old.y + old.height) > Screen.height - dist;

			if(left) r.x = Math.Max(0, old.x);
			if(top) r.y = Math.Max(0, old.y);
			if(right) r.x = Math.Min(Screen.width - r.width, old.x + old.width - r.width);
			if(bottom) r.y = Math.Min(Screen.height - r.height, old.y + old.height - r.height);
		}

		private static RemoteView anomalyView;
		public static void gui_instruments_build(int wid) {
			GUI.skin = null;
			int parts = 0;
			style_readout.alignment = TextAnchor.MiddleCenter;
			style_readout.fontSize = 40;
			style_readout.normal.textColor = c_good;
			SCANdata.SCANtype sensors = SCANcontroller.controller.activeSensorsOnVessel(FlightGlobals.ActiveVessel.id);
			SCANdata data = SCANcontroller.controller.getData(FlightGlobals.currentMainBody);

			if(maptraq_frame >= Time.frameCount - 5) {
				if(data.isCovered(FlightGlobals.ActiveVessel.longitude, FlightGlobals.ActiveVessel.latitude, SCANdata.SCANtype.AltimetryHiRes)) {
					sensors |= SCANdata.SCANtype.Altimetry;
				}
				if(data.isCovered(FlightGlobals.ActiveVessel.longitude, FlightGlobals.ActiveVessel.latitude, SCANdata.SCANtype.Biome)) {
					sensors |= SCANdata.SCANtype.Biome;
				}
			}

			GUILayout.BeginVertical();

			if(notMappingToday) {
				style_readout.normal.textColor = c_bad;
				GUILayout.Label("NO DATA", style_readout);
			} else {
				if((sensors & SCANdata.SCANtype.Biome) != SCANdata.SCANtype.Nothing) {
					GUILayout.Label(data.getBiomeName(FlightGlobals.ActiveVessel.longitude, FlightGlobals.ActiveVessel.latitude), style_readout);
					++parts;
				}

				if((sensors & SCANdata.SCANtype.AltimetryHiRes) != SCANdata.SCANtype.Nothing) {
					double h = FlightGlobals.ActiveVessel.heightFromTerrain;
					if(h < 0) h = FlightGlobals.ActiveVessel.altitude;
					GUILayout.Label(h.ToString("N2") + "m", style_readout);
					++parts;
				}

				if((sensors & SCANdata.SCANtype.AnomalyDetail) != SCANdata.SCANtype.Nothing) {
					SCANdata.SCANanomaly nearest = null;
					double nearest_dist = -1;
					foreach(SCANdata.SCANanomaly a in data.getAnomalies()) {
						if(!a.known) continue;
						double d = (a.mod.transform.position - FlightGlobals.ActiveVessel.transform.position).magnitude;
						if(d < nearest_dist || nearest_dist < 0) {
							if(d < 50000) {
								nearest = a;
								nearest_dist = d;
							}
						}
					}
					if(nearest != null) {
						string txt = "Anomaly";
						if(nearest.detail) txt = nearest.name;
						txt += " " + distanceString(nearest_dist);
						GUILayout.Label(txt, style_readout);
						++parts;

						if(anomalyView == null) anomalyView = new RemoteView();
						if(anomalyView != null) {
							if(nearest.mod != null) {
								if(anomalyView.lookat != nearest.mod.gameObject) anomalyView.setup(320, 240, nearest.mod.gameObject);
								Texture t = anomalyView.getTexture();
								if(t != null) {
									GUILayout.Label(anomalyView.getTexture());
									style_overlay.fontSize = 36;
									style_overlay.normal.textColor = cb_skyBlue;
									style_overlay.fontStyle = FontStyle.Bold;
									anomalyView.drawOverlay(GUILayoutUtility.GetLastRect(), style_overlay, nearest.detail);
								}
							} 
						}
					}
				}

				if(parts <= 0) {
					style_readout.normal.textColor = c_bad;
					GUILayout.Label("NO DATA", style_readout);
				}
			}

			GUILayout.EndVertical();

			Rect r = new Rect(pos_instruments.width - 25, 0, 22, 22);
			style_button.normal.textColor = cb_vermillion;
			if(GUI.Button(r, SCANcontroller.controller.closeBox, style_button)) instruments_visible = false;

			GUI.DragWindow();
		}

		public static string distanceString(double dist) {
			if(dist < 5000) return dist.ToString("N1") + "m";
			return (dist / 1000d).ToString("N3") + "km";
		}

		private static string[] exmarks = {"✗", "✘", "×", "✖", "x", "X", "∇", "☉", "★", "*", "•", "º", "+"};
		private static string[] twnames = {"Off", "Low", "Medium", "High"};
		private static int[] twvals = { 1, 7, 11, 20 };
		public static void gui_settings_build(int wid) {
			GUI.skin = null;
			GUILayout.BeginVertical();
			GUILayout.Space(8);

			// anomaly marker & close widget
			GUILayout.Label("X Marks", style_headline);
			GUILayout.BeginHorizontal();
			GUILayout.BeginVertical();
			GUILayout.Label("Anomaly Marker");
			GUILayout.Label("Close Widget");
			GUILayout.EndVertical();
			for(int i=0; i<exmarks.Length; ++i) {
				GUILayout.BeginVertical();
				style_button.normal.textColor = SCANcontroller.controller.anomalyMarker == exmarks[i] ? c_good : Color.white;
				if(GUILayout.Button(exmarks[i], style_button)) {
					SCANcontroller.controller.anomalyMarker = exmarks[i];
				}
				style_button.normal.textColor = SCANcontroller.controller.closeBox == exmarks[i] ? c_good : Color.white;
				if(GUILayout.Button(exmarks[i], style_button)) {
					SCANcontroller.controller.closeBox = exmarks[i];
				}
				GUILayout.EndVertical();
			}
			GUILayout.EndHorizontal();

			// background scanning
			GUILayout.Space(16);
			GUILayout.Label("Background Scanning", style_headline);
			SCANcontroller.controller.scan_background = GUILayout.Toggle(SCANcontroller.controller.scan_background, "Scan all active celestials");

			// scanning for individual SoIs
			GUILayout.BeginHorizontal();
			int count = 0;
			foreach(CelestialBody body in FlightGlobals.Bodies) {
				if(count == 0) GUILayout.BeginVertical();
				SCANdata data = SCANcontroller.controller.getData(body);
				data.disabled = !GUILayout.Toggle(!data.disabled, body.bodyName + " (" + data.getCoveragePercentage(SCANdata.SCANtype.Nothing).ToString("N1") + "%)");
				if(count >= 4) {
					GUILayout.EndVertical();
					count = 0;
				} else {
					++count;
				}
			}
			if(count != 0) GUILayout.EndVertical();
			GUILayout.EndHorizontal();

			// time warp resolution
			GUILayout.Space(16);
			GUILayout.Label("Time Warp Resolution", style_headline);
			GUILayout.BeginHorizontal();
			for(int i=0; i<twnames.Length; ++i) {
				style_button.normal.textColor = Color.white;
				if(SCANcontroller.controller.timeWarpResolution == twvals[i]) style_button.normal.textColor = c_good;
				if(GUILayout.Button(twnames[i], style_button)) {
					SCANcontroller.controller.timeWarpResolution = twvals[i];
				}
			}
			GUILayout.EndHorizontal();
			string txt = "Sensors: " + SCANcontroller.activeSensors.ToString() + " Vessels: " + SCANcontroller.activeVessels.ToString() + " Passes: " + SCANcontroller.controller.actualPasses.ToString();
			GUILayout.Label(txt);

			// data management
			GUILayout.Space(16);
			GUILayout.Label("Data Management", style_headline);
			GUILayout.BeginHorizontal();
			if(GUILayout.Button("Reset map of " + FlightGlobals.currentMainBody.theName)) {
				SCANdata data = SCANcontroller.controller.getData(FlightGlobals.currentMainBody);
				data.reset();
			}
			if(GUILayout.Button("Reset <b>all</b> data")) {
				foreach(SCANdata data in SCANcontroller.controller.body_data.Values) {
					data.reset();
				}
			}
			GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal();
			if(GUILayout.Button("Reset window positions")) {
				pos_bigmap.x = 0;
				pos_bigmap.y = 0;
				SCANcontroller.controller.map_x = 100;
				SCANcontroller.controller.map_y = 50;
				SCANcontroller.controller.map_width = 0;
				pos_infobox.x = 0;
				pos_infobox.y = 0;
				pos_instruments.x = -1;
				pos_instruments.y = 0;
			}
			GUILayout.EndHorizontal();

			GUILayout.Space(16);
			if(GUILayout.Button("Close")) {
				settings_visible = false;
			}
			GUILayout.EndVertical();
			GUI.DragWindow();
		}

		static double startUT;
		private static bool mapPosAtT(Rect maprect, SCANmap map, ref Rect r, Vessel vessel, Orbit o, double dT) {
			double UT = startUT + dT;
			if(double.IsNaN(UT)) return false;
			try {
				if(double.IsNaN(o.getObtAtUT(UT))) return false;
				Vector3d pos = o.getPositionAtUT(UT);
				double rotation = 0;
				if(vessel.mainBody.rotates) {
					rotation = (360 * (dT / vessel.mainBody.rotationPeriod)) % 360;
				}
				double lo = (vessel.mainBody.GetLongitude(pos) - rotation);
				double la = (vessel.mainBody.GetLatitude(pos));
				double lon = (map.projectLongitude(lo, la) + 180) % 360;
				double lat = (map.projectLatitude(lo, la) + 90) % 180;
				lat = map.scaleLatitude(lat);
				lon = map.scaleLongitude(lon);
				if(lat < 0 || lon < 0 || lat > 180 || lon > 360) return false;
				lon = lon * maprect.width / 360f;
				lat = maprect.height - lat * maprect.height / 180f;
				r.x = maprect.x + (float)lon;
				r.y = maprect.y + (float)lat;
				return true;
			} catch(Exception) {
				return false;
			}
		}

		private static int[] eq_an_map, eq_dn_map;
		private static Texture2D eq_map;
		private static int eq_frame = 0;

		private static void drawOrbit(Rect maprect, SCANmap map, Vessel vessel) {
			if(vessel.LandedOrSplashed) return;
			bool lite = maprect.width < 400;
			Orbit o = vessel.orbit;
			startUT = Planetarium.GetUniversalTime();
			double UT = startUT;
			int steps = 100; // increase for neater lines, decrease for better speed indication
			bool ath = false;
			if(vessel.mainBody.atmosphere) {
				if(vessel.mainBody.maxAtmosphereAltitude >= vessel.altitude) {
					ath = true;
				}
			}
			Rect r = new Rect(0, 0, 50f, 50f);
			Color col;
			// project the last and the current orbital period onto the map
			for(int i=-steps; i<steps; ++i) {
				if(i < 0) UT = startUT - (steps + i) * (o.period / steps);
				else UT = startUT + i * o.period * 1f / steps;
				if(double.IsNaN(UT)) continue;
				if(UT < o.StartUT && o.StartUT != startUT) continue;
				if(UT > o.EndUT) continue;
				if(double.IsNaN(o.getObtAtUT(UT))) continue;
				Vector3d pos = o.getPositionAtUT(UT);
				double rotation = 0;
				if(vessel.mainBody.rotates) {
					rotation = (360 * ((UT - startUT) / vessel.mainBody.rotationPeriod)) % 360;
				}
				double alt = (vessel.mainBody.GetAltitude(pos));
				if(alt < 0) {
					if(i < 0) {
						i = 0;
						continue;
					}
					break;
				}
				double lo = (vessel.mainBody.GetLongitude(pos) - rotation);
				double la = (vessel.mainBody.GetLatitude(pos));
				double lon = (map.projectLongitude(lo, la) + 180) % 360;
				double lat = (map.projectLatitude(lo, la) + 90) % 180;
				lat = map.scaleLatitude(lat);
				lon = map.scaleLongitude(lon);
				if(lat < 0 || lon < 0 || lat > 180 || lon > 360) continue;
				lon = lon * maprect.width / 360f;
				lat = maprect.height - lat * maprect.height / 180f;
				r.x = maprect.x + (float)lon;
				r.y = maprect.y + (float)lat;
				col = cb_skyBlue;
				if(i < 0) {
					col = cb_orange;
				} else {
					if(vessel.mainBody.atmosphere) {
						if(vessel.mainBody.maxAtmosphereAltitude >= alt) {
							if(!ath) {
								ath = true;
								// do something when it flips?
							}
							col = cb_reddishPurple;
						}
					}
				}
				drawOrbitIcon((int)r.x, (int)r.y, OrbitIcon.Planet, col, 8, false);
			}

			// show apoapsis and periapsis
			if(o.ApA > 0 && mapPosAtT(maprect, map, ref r, vessel, o, o.timeToAp)) {
				drawOrbitIcon((int)r.x, (int)r.y, OrbitIcon.Ap, cb_skyBlue, 32, true);
				r.x += 24;
				r.y -= 12;
				if(!lite) drawLabel(r, cb_skyBlue, o.ApA.ToString("N1"), true, true);
			}
			if(o.PeA > 0 && mapPosAtT(maprect, map, ref r, vessel, o, o.timeToPe)) {
				drawOrbitIcon((int)r.x, (int)r.y, OrbitIcon.Pe, cb_skyBlue, 32, true);
				r.x += 24;
				r.y -= 12;
				if(!lite) drawLabel(r, cb_skyBlue, o.PeA.ToString("N1"), true, true);
			}

			if(lite) return;

			// show first maneuver node
			if(vessel.patchedConicSolver.maneuverNodes.Count > 0) {
				ManeuverNode n = vessel.patchedConicSolver.maneuverNodes[0];
				if(n.patch == vessel.orbit && n.nextPatch != null && n.nextPatch.activePatch && n.UT > startUT - o.period && mapPosAtT(maprect, map, ref r, vessel, o, n.UT - startUT)) {
					col = cb_reddishPurple;
					if(SCANcontroller.controller.colours != 1) col = XKCDColors.PurplyPink;
					drawOrbitIcon((int)r.x, (int)r.y, OrbitIcon.ManeuverNode, col, 32, true);
					Orbit nuo = n.nextPatch;
					for(int i=0; i<steps; ++i) {
						double T = n.UT - startUT + i * nuo.period / steps;
						if(T + startUT > nuo.EndUT) break;
						if(mapPosAtT(maprect, map, ref r, vessel, nuo, T)) {
							drawOrbitIcon((int)r.x, (int)r.y, OrbitIcon.Planet, col, 8, false);
						}
					}
					if(nuo.patchEndTransition == Orbit.PatchTransitionType.ESCAPE) {
						drawOrbitIcon((int)r.x, (int)r.y, OrbitIcon.Exit, col, 32, true);
					} else if(nuo.patchEndTransition == Orbit.PatchTransitionType.ENCOUNTER) {
						drawOrbitIcon((int)r.x, (int)r.y, OrbitIcon.Encounter, col, 32, true);
					}
					if(nuo.timeToAp > 0 && n.UT + nuo.timeToAp < nuo.EndUT && mapPosAtT(maprect, map, ref r, vessel, nuo, n.UT - startUT + nuo.timeToAp)) {
						drawOrbitIcon((int)r.x, (int)r.y, OrbitIcon.Ap, col, 32, true);
					}
					if(nuo.timeToPe > 0 && n.UT + nuo.timeToPe < nuo.EndUT && mapPosAtT(maprect, map, ref r, vessel, nuo, n.UT - startUT + nuo.timeToPe)) {
						drawOrbitIcon((int)r.x, (int)r.y, OrbitIcon.Pe, col, 32, true);
					}
				}
			}

			if(o.PeA < 0) return;
			if(overlay_static == null) return;
			if(map.projection == SCANmap.MapProjection.Polar) return;

			if(eq_frame <= 0) {
				// predict equatorial crossings for the next 100 loops
				double TAAN = 360f - o.argumentOfPeriapsis;	// true anomaly at ascending node
				double TADN = (TAAN + 180) % 360;			// true anomaly at descending node
				double MAAN = meanForTrue(TAAN, o.eccentricity);
				double MADN = meanForTrue(TADN, o.eccentricity);
				double tAN = (((MAAN - o.meanAnomaly * Mathf.Rad2Deg + 360) % 360) / 360f * o.period + startUT);
				double tDN = (((MADN - o.meanAnomaly * Mathf.Rad2Deg + 360) % 360) / 360f * o.period + startUT);

				int eqh = 16;
				if(eq_an_map == null || eq_dn_map == null || eq_an_map.Length != overlay_static.width) {
					eq_an_map = new int[overlay_static.width];
					eq_dn_map = new int[overlay_static.width];
				}
				if(eq_map == null || eq_map.width != eq_an_map.Length) {
					eq_map = new Texture2D(eq_an_map.Length, eqh, TextureFormat.ARGB32, false);
				}
				for(int i=0; i<eq_an_map.Length; ++i) {
					eq_an_map[i] = 0;
					eq_dn_map[i] = 0;
				}
				for(int i=0; i<100; ++i) {
					double UTAN = tAN + o.period * i;
					double UTDN = tDN + o.period * i;
					if(double.IsNaN(UTAN) || double.IsNaN(UTDN)) continue;
					Vector3d pAN = o.getPositionAtUT(UTAN);
					Vector3d pDN = o.getPositionAtUT(UTDN);
					double rotAN = 0, rotDN = 0;
					if(vessel.mainBody.rotates) {
						rotAN = ((360 * ((UTAN - startUT) / vessel.mainBody.rotationPeriod)) % 360);
						rotDN = ((360 * ((UTDN - startUT) / vessel.mainBody.rotationPeriod)) % 360);
					}
					double loAN = vessel.mainBody.GetLongitude(pAN) - rotAN;
					double loDN = vessel.mainBody.GetLongitude(pDN) - rotDN;
					int lonAN = (int)(((map.projectLongitude(loAN, 0) + 180) % 360) * eq_an_map.Length / 360f);
					int lonDN = (int)(((map.projectLongitude(loDN, 0) + 180) % 360) * eq_dn_map.Length / 360f);
					if(lonAN >= 0 && lonAN < eq_an_map.Length) eq_an_map[lonAN] += 1;
					if(lonDN >= 0 && lonDN < eq_dn_map.Length) eq_dn_map[lonDN] += 1;
				}
				Color[] pix = eq_map.GetPixels(0, 0, eq_an_map.Length, eqh);
				Color cAN = cb_skyBlue, cDN = cb_orange;
				for(int y = 0; y < eqh; ++y) {
					Color lc = Color.clear;
					for(int x = 0; x < eq_an_map.Length; ++x) {
						Color c = Color.clear;
						float scale = 0;
						if(y < eqh / 2) {
							c = cDN;
							scale = eq_dn_map[x];
						} else {
							c = cAN;
							scale = eq_an_map[x];
						}
						if(scale >= 1) {
							if(y == 0 || y == eqh - 1) {
								c = Color.black;
							} else {
								if(lc == Color.clear) pix[y * eq_an_map.Length + x - 1] = Color.black;
								scale = Mathf.Clamp(scale - 1, 0, 10) / 10f;
								c = Color.Lerp(c, Color.white, scale);
							}
						} else {
							c = Color.clear;
							if(lc != Color.clear && lc != Color.black) c = Color.black;
						}
						pix[y * eq_an_map.Length + x] = c;
						lc = c;
					}
				}
				eq_map.SetPixels(0, 0, eq_an_map.Length, eqh, pix);
				eq_map.Apply();
				eq_frame = 4;
			} else {
				eq_frame -= 1;
			}

			if(eq_map != null) {
				r.x = maprect.x;
				r.y = maprect.y + maprect.height / 2 + - eq_map.height / 2;
				r.width = eq_map.width;
				r.height = eq_map.height;
				GUI.DrawTexture(r, eq_map);
			}
		}

		private static double meanForTrue(double TA, double e) {
			TA = TA * Mathf.Deg2Rad;
			double EA = Math.Acos((e + Math.Cos(TA)) / (1 + e * Math.Cos(TA)));
			if(TA > Math.PI) EA = 2 * Math.PI - EA;
			double MA = EA - e * Math.Sin(EA);
			// the mean anomaly isn't really an angle, but I'm a simple person
			return MA * Mathf.Rad2Deg;
		}

		private static void drawGrid(Rect maprect, SCANmap map) {
			int x, y;
			for(double lat=-90; lat<90; lat += 2) {
				for(double lon=-180; lon<180; lon += 2) {
					if(lat % 30 == 0 || lon % 30 == 0) {
						x = (int)(map.mapscale * ((map.projectLongitude(lon, lat) + 180) % 360));
						y = (int)(map.mapscale * ((map.projectLatitude(lon, lat) + 90) % 180));
						drawDot(x, y, Color.white, overlay_static);
					}
				}
			}
		}

		private static void drawMapLabels(Rect maprect, Vessel vessel, SCANmap map, SCANdata data) {
			foreach(Vessel v in FlightGlobals.Vessels) {
				if(v.mainBody == vessel.mainBody) {
					if(v.vesselType == VesselType.Flag && SCANcontroller.controller.map_flags) {
						drawVesselLabel(maprect, map, 0, v);
					}
				}
			}
			if(SCANcontroller.controller.map_markers) {
				foreach(SCANdata.SCANanomaly anomaly in data.getAnomalies()) {
					drawAnomalyLabel(maprect, map, anomaly);
				}
			}
			drawVesselLabel(maprect, map, 0, vessel);
		}

		private static void drawDot(int x, int y, Color c, Texture2D tex) {
			tex.SetPixel(x, y, c);
			tex.SetPixel(x - 1, y, Color.black);
			tex.SetPixel(x + 1, y, Color.black);
			tex.SetPixel(x, y - 1, Color.black);
			tex.SetPixel(x, y + 1, Color.black);
		}

		private static void clearTexture(Texture2D tex) {
			Color[] pix = tex.GetPixels();
			for(int i=0; i<pix.Length; ++i) pix[i] = Color.clear;
			tex.SetPixels(pix);
		}

		private static void drawLegendLabel(Rect r, float val, float min, float max) {
			if(val < min || val > max) return;
			float scale = r.width * 1f / (max - min);
			float x = r.x + scale * (val - min);
			Rect lr = new Rect(x, r.y + r.height/4, r.width-x, r.height);
			drawLabel(lr, Color.white, "|", true, true);
			string txt = val.ToString("N0");
			GUIContent c = new GUIContent(txt);
			Vector2 dim = style_label.CalcSize(c);
			lr.y += dim.y * 0.25f;
			lr.x -= dim.x / 2;
			if(lr.x < r.x) lr.x = r.x;
			if(lr.x + dim.x > r.x + r.width) lr.x = r.x + r.width - dim.x;
			drawLabel(lr, Color.white, txt, false, true);
		}

		private static Rect drawLegend() {
			if(bigmap.mapmode == 0 && SCANcontroller.controller.legend) {
				GUILayout.Label("", GUILayout.ExpandWidth(true));
				Rect r = GUILayoutUtility.GetLastRect();
				r.width -= 64;
				GUI.DrawTexture(r, SCANmap.getLegend(-1500f, 9000f, SCANcontroller.controller.colours));
				for(float val=-1000f; val < 9000f; val += 1000f) {
					drawLegendLabel(r, val, -1500f, 9000f);
				}
				return r;
			}return new Rect(0,0,0,0);
		}

		private static void gui_bigmap_build(int wid) {
			bool repainting = Event.current.type == EventType.Repaint;
			GUI.skin = null;

			// Handle dragging independently of mouse events: MouseDrag doesn't work
			// so well if the resizer widget can't follow the mouse because the map
			// aspect ratio is constrained.
			if(bigmap_dragging && !repainting) {
				if(Input.GetMouseButtonUp(0)) {
					bigmap_dragging = false;
					if(bigmap_drag_w < 400) bigmap_drag_w = 400;
					bigmap.setWidth((int)bigmap_drag_w);
					overlay_static = null;
					SCANcontroller.controller.map_width = bigmap.mapwidth;
				} else {
					float xx = Input.mousePosition.x;
					bigmap_drag_w += xx - bigmap_drag_x;
					bigmap_drag_x = xx;
				}
				if(Event.current.isMouse) Event.current.Use();
			}

			Vessel vessel = FlightGlobals.ActiveVessel;
			GUILayout.BeginVertical();

			SCANdata data = SCANcontroller.controller.getData(vessel.mainBody);
			Texture2D map = bigmap.getPartialMap();

			float dw = bigmap_drag_w;
			if(dw < 400) dw = 400;
			float dh = dw / 2f;
			if(bigmap_dragging) {
				GUILayout.Label("", GUILayout.Width(dw), GUILayout.Height(dh));
			} else {
				GUILayout.Label("", GUILayout.Width(map.width), GUILayout.Height(map.height));
			}
			Rect maprect = GUILayoutUtility.GetLastRect();
			maprect.width = bigmap.mapwidth;
			maprect.height = bigmap.mapheight;

			if(overlay_static == null) {
				overlay_static = new Texture2D((int)bigmap.mapwidth, (int)bigmap.mapheight, TextureFormat.ARGB32, false);
				overlay_static_dirty = true;
			}

			if(overlay_static_dirty) {
				clearTexture(overlay_static);
				if(SCANcontroller.controller.map_grid) {
					drawGrid(maprect, bigmap);
				}
				overlay_static.Apply();
				overlay_static_dirty = false;
			}
		
			if(bigmap_dragging) {
				maprect.width = dw;
				maprect.height = dh;
				GUI.DrawTexture(maprect, map, ScaleMode.StretchToFill);
			} else {
				GUI.DrawTexture(maprect, map);
			}

			if(overlay_static != null) {
				GUI.DrawTexture(maprect, overlay_static, ScaleMode.StretchToFill);
			}

			if(bigmap.projection == SCANmap.MapProjection.Polar) {
				rc.x = maprect.x + maprect.width / 2 - maprect.width / 8;
				rc.y = maprect.y + maprect.height / 8;
				drawLabel(rc, Color.white, "S", true, true);
				rc.x = maprect.x + maprect.width / 2 + maprect.width / 8;
				drawLabel(rc, Color.white, "N", true, true);
			}

			if(SCANcontroller.controller.map_orbit && !notMappingToday) {
				drawOrbit(maprect, bigmap, vessel);
			}

			GUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
			GUILayout.BeginHorizontal(GUILayout.Width(300));


			GUILayout.BeginVertical();
			if(GUILayout.Button("Close")) {
				bigmap_visible = false;
			}
			GUILayout.FlexibleSpace();
			style_button.normal.textColor = Color.grey;
			if(bigmap.isMapComplete()) style_button.normal.textColor = Color.white;
			if(GUILayout.Button("Export PNG", style_button)) {
				if(bigmap.isMapComplete()) {
					bigmap.exportPNG();
				}
			}
			GUILayout.EndVertical();

			GUILayout.BeginVertical();
			style_button.normal.textColor = SCANcontroller.controller.colours == 1 ? c_good : Color.white;
			if(GUILayout.Button("Grey", style_button)) {
				SCANcontroller.controller.colours = 1;
				data.resetImages();
				bigmap.resetMap();
			}
			style_button.normal.textColor = SCANcontroller.controller.colours == 0 ? c_good : Color.white;
			if(GUILayout.Button("Colour", style_button)) {
				SCANcontroller.controller.colours = 0;
				data.resetImages();
				bigmap.resetMap();
			}
			style_button.normal.textColor = SCANcontroller.controller.legend ? c_good : Color.white;
			if(GUILayout.Button("Legend", style_button)) {
				SCANcontroller.controller.legend = !SCANcontroller.controller.legend;
			}
			GUILayout.EndVertical();

			GUILayout.BeginVertical();
			style_button.normal.textColor = bigmap.mapmode == 0 ? c_good : Color.white;
			if(GUILayout.Button("Altimetry", style_button)) {
				bigmap.resetMap(0);
			}
			style_button.normal.textColor = bigmap.mapmode == 1 ? c_good : Color.white;
			if(GUILayout.Button("Slope", style_button)) {
				bigmap.resetMap(1);
			}
			style_button.normal.textColor = bigmap.mapmode == 2 ? c_good : Color.white;
			if(GUILayout.Button("Biome", style_button)) {
				bigmap.resetMap(2);
			}
			GUILayout.EndVertical();

			GUILayout.BeginVertical();
			GUILayout.BeginHorizontal();
			style_button.normal.textColor = SCANcontroller.controller.map_markers ? c_good : Color.white;
			if(GUILayout.Button("Markers", style_button)) {
				SCANcontroller.controller.map_markers = !SCANcontroller.controller.map_markers;
			}
			style_button.normal.textColor = SCANcontroller.controller.map_flags ? c_good : Color.white;
			if(GUILayout.Button("Flags", style_button)) {
				SCANcontroller.controller.map_flags = !SCANcontroller.controller.map_flags;
			}
			GUILayout.EndHorizontal();
			style_button.normal.textColor = SCANcontroller.controller.map_orbit ? c_good : Color.white;
			if(GUILayout.Button("Orbit", style_button)) {
				SCANcontroller.controller.map_orbit = !SCANcontroller.controller.map_orbit;
			}
			style_button.normal.textColor = SCANcontroller.controller.map_grid ? c_good : Color.white;
			if(GUILayout.Button("Grid", style_button)) {
				SCANcontroller.controller.map_grid = !SCANcontroller.controller.map_grid;
				overlay_static_dirty = true;
			}
			GUILayout.EndVertical();

			GUILayout.BeginVertical();
			for(int i=0; i<SCANmap.projectionNames.Length; ++i) {
				style_button.normal.textColor = (int)bigmap.projection == i ? c_good : Color.white;
				if(GUILayout.Button(SCANmap.projectionNames[i], style_button)) {
					bigmap.setProjection((SCANmap.MapProjection)i);
					SCANcontroller.controller.projection = i;
					overlay_static_dirty = true;
				}
			}
			GUILayout.EndVertical();
			GUILayout.EndHorizontal();

			string info = "";
			float mx = Event.current.mousePosition.x - maprect.x;
			float my = Event.current.mousePosition.y - maprect.y;
			bool in_map = false, in_spotmap = false;
			double mlon = 0, mlat = 0;
			if(mx >= 0 && my >= 0 && mx < map.width && my < map.height && !bigmap_dragging) {
				double mlo = (mx * 360f / map.width) - 180;
				double mla = 90 - (my * 180f / map.height);
				mlon = bigmap.unprojectLongitude(mlo, mla);
				mlat = bigmap.unprojectLatitude(mlo, mla);

				if(spotmap != null) {
					if(mx >= pos_spotmap.x - maprect.x && my >= pos_spotmap.y - maprect.y && mx <= pos_spotmap.x + pos_spotmap.width - maprect.x && my <= pos_spotmap.y + pos_spotmap.height - maprect.y) {
						in_spotmap = true;
						mlon = spotmap.lon_offset + ((mx - pos_spotmap.x + maprect.x) / spotmap.mapscale) - 180;
						mlat = spotmap.lat_offset + ((pos_spotmap.height - (my - pos_spotmap.y + maprect.y)) / spotmap.mapscale) - 90;
						if(mlat > 90) {
							mlon = (mlon + 360) % 360 - 180;
							mlat = 180 - mlat;
						} else if(mlat < -90) {
							mlon = (mlon + 360) % 360 - 180;
							mlat = -180 - mlat;
						}
					}
				}

				if(mlon >= -180 && mlon <= 180 && mlat >= -90 && mlat <= 90) {
					in_map = true;
					if(data.isCovered(mlon, mlat, SCANdata.SCANtype.AltimetryLoRes)) {
						if(vessel.mainBody.pqsController == null) info += colored(c_ugly, "LO ");
						else info += colored(c_good, "LO ");
					} else info += "<color=\"grey\">LO</color> ";
					if(data.isCovered(mlon, mlat, SCANdata.SCANtype.AltimetryHiRes)) {
						if(vessel.mainBody.pqsController == null) info += colored(c_ugly, "HI ");
						else info += colored(c_good, "HI ");
					} else info += "<color=\"grey\">HI</color> ";
					if(data.isCovered(mlon, mlat, SCANdata.SCANtype.Biome)) {
						if(vessel.mainBody.BiomeMap == null || vessel.mainBody.BiomeMap.Map == null) info += colored(c_ugly, "BIO ");
						else info += colored(c_good, "BIO ");
					} else info += "<color=\"grey\">BIO</color> ";
					if(data.isCovered(mlon, mlat, SCANdata.SCANtype.Anomaly)) info += colored(c_good, "ANOM ");
					else info += "<color=\"grey\">ANOM</color> ";
					if(data.isCovered(mlon, mlat, SCANdata.SCANtype.AnomalyDetail)) info += colored(c_good, "BTDT ");
					else info += "<color=\"grey\">BTDT</color> ";
					if(data.isCovered(mlon, mlat, SCANdata.SCANtype.AltimetryHiRes)) {
						info += "<b>" + data.getElevation(mlon, mlat).ToString("N2") + "m</b> ";
					} else if(data.isCovered(mlon, mlat, SCANdata.SCANtype.AltimetryLoRes)) {
						info += "<b>~" + (((int)data.getElevation(mlon, mlat) / 500) * 500).ToString() + "m</b> ";
					}
					if(data.isCovered(mlon, mlat, SCANdata.SCANtype.Biome)) {
						info += data.getBiomeName(mlon, mlat) + " ";
					}
					info += "\n" + toDMS(mlat, mlon) + " (lat: " + mlat.ToString("F2") + " lon: " + mlon.ToString("F2") + ") ";
					if(in_spotmap) info += " " + spotmap.mapscale.ToString("F1") + "x";
				} else {
					info += " " + mlat.ToString("F") + " " + mlon.ToString("F"); // uncomment for debugging projections
				}
			}
			
			Rect legend_rect;
			if(maprect.width < 720) {
				GUILayout.EndHorizontal();
				readableLabel(info, Color.white);
				legend_rect = drawLegend();
			} else {
				GUILayout.BeginVertical();
				readableLabel(info, Color.white);
				legend_rect = drawLegend();
				GUILayout.EndVertical();
				GUILayout.EndHorizontal();
			}
			bool in_gradient_legend = false;
			float gradient_height = -1;
			if(legend_rect.width>0f) {
				float mgx = (Event.current.mousePosition.x - legend_rect.x) / legend_rect.width;
				float mgy = (Event.current.mousePosition.y - legend_rect.y) / legend_rect.height;
				in_gradient_legend = (mgx>=0f) && (mgx<=1f) && (mgy>=0f) && (mgy<=1f);
				gradient_height = -1500f+(mgx*10500f);
			}

			if(!notMappingToday) drawMapLabels(maprect, vessel, bigmap, data);

			if(spotmap != null) {
				spotmap.setBody(vessel.mainBody);
				GUI.Box(pos_spotmap, spotmap.getPartialMap());
				if(!notMappingToday) {
					drawOrbit(pos_spotmap, spotmap, vessel);
					drawMapLabels(pos_spotmap, vessel, spotmap, data);
				}
				pos_spotmap_x.x = pos_spotmap.x + pos_spotmap.width + 4;
				pos_spotmap_x.y = pos_spotmap.y;
				style_button.normal.textColor = cb_vermillion;
				if(GUI.Button(pos_spotmap_x, SCANcontroller.controller.closeBox, style_button)) {
					spotmap = null;
				}
			}

			Rect fpswidget = new Rect(maprect.x + maprect.width - 32, maprect.y + maprect.height + 32, 32, 24);
			GUI.Label(fpswidget, fps.ToString("N1"));

			Rect resizer = new Rect(maprect.x + maprect.width - 24, maprect.y + maprect.height + 8, 24, 24);
			GUI.Box(resizer, "//");
			if(Event.current.isMouse) {
				if(Event.current.type == EventType.MouseUp) {
					if(bigmap_dragging) {
					} else if(Event.current.button == 1) {
						if(in_map || in_spotmap) {
							if(spotmap == null) {
								spotmap = new SCANmap();
								spotmap.setSize(180, 180);
							}
							if(in_spotmap) {
								spotmap.mapscale = spotmap.mapscale * 1.25f;
							} else {
								spotmap.mapscale = 10;
							}
							spotmap.centerAround(mlon, mlat);
							spotmap.resetMap(bigmap.mapmode);
							pos_spotmap.width = 180;
							pos_spotmap.height = 180;
							if(!in_spotmap) {
								pos_spotmap.x = Event.current.mousePosition.x - pos_spotmap.width / 2;
								pos_spotmap.y = Event.current.mousePosition.y - pos_spotmap.height / 2;
								if(mx > maprect.width / 2) pos_spotmap.x -= pos_spotmap.width;
								else pos_spotmap.x += pos_spotmap.height;
								pos_spotmap.x = Math.Max(maprect.x, Math.Min(maprect.x + maprect.width - pos_spotmap.width, pos_spotmap.x));
								pos_spotmap.y = Math.Max(maprect.y, Math.Min(maprect.y + maprect.height - pos_spotmap.height, pos_spotmap.y));
							}
						}
						if (in_gradient_legend)
						{
							SCANmap.heightGradientRange = gradient_height-SCANmap.startGradientHeight;
							bigmap.resetMap();
							if(spotmap != null) spotmap.resetMap();
						}
					} else if(Event.current.button == 0) {
						if(spotmap != null && in_spotmap) {
							spotmap.mapscale = spotmap.mapscale / 1.25f;
							if(spotmap.mapscale < 10) spotmap.mapscale = 10;
							spotmap.resetMap(spotmap.mapmode);
							Event.current.Use();
						}
						if (in_gradient_legend)
						{
							SCANmap.startGradientHeight = gradient_height;
							bigmap.resetMap();
							if(spotmap != null) spotmap.resetMap();
						}
					}
					Event.current.Use();
				} else if(Event.current.type == EventType.MouseDown) { 
					if(Event.current.button == 0) {
						if(resizer.Contains(Event.current.mousePosition)) {
							bigmap_dragging = true;
							bigmap_drag_x = Input.mousePosition.x;
							bigmap_drag_w = bigmap.mapwidth;
							Event.current.Use();
						}
					}
				}
			} 

			GUILayout.EndVertical();
			GUI.DragWindow();
		}

		private static float fps_time_passed, fps, fps_sum, fps_frames;

		public static void gui_show() {
			bool repainting = Event.current.type == EventType.Repaint;
			Vessel vessel = FlightGlobals.ActiveVessel;

			if(Time.frameCount - gui_frame_ping > 5) {
				gui_active = false;
				RenderingManager.RemoveFromPostDrawQueue(3, guicb);
			}
			if(Time.frameCount > gui_frame_draw) {
				fps_time_passed += Time.deltaTime;
				fps_frames += 1;
				fps_sum += Time.timeScale / Time.deltaTime;
				if(fps_time_passed >= 1) {
					fps = fps_sum / fps_frames;
					fps_time_passed = 0;
					fps_frames = 0;
					fps_sum = 0;
				}
			}
			gui_frame_draw = Time.frameCount;

			if(style_headline == null) {
				style_headline = new GUIStyle();
				style_headline.normal.textColor = XKCDColors.YellowGreen;
				style_headline.alignment = TextAnchor.MiddleCenter;
				style_headline.fontSize = 40;
			}
			if(style_readout == null) {
				style_readout = new GUIStyle();
			}
			if(style_overlay == null) {
				style_overlay = new GUIStyle();
			}
			if(dotty == null) {
				ScreenMessages sm = (ScreenMessages)GameObject.FindObjectOfType(typeof(ScreenMessages));
				dotty = sm.textStyles[1].font;
				style_headline.font = dotty;
				style_readout.font = dotty;
				style_overlay.font = dotty;
			}

			if(style_button == null) {
				style_button = new GUIStyle(GUI.skin.button);
			}
			if(style_toggle == null) {
				style_toggle = new GUIStyle(GUI.skin.toggle);
			}

			GUI.skin = null; // and by this, I mean "set it to the default"
			GUI.skin.label.wordWrap = false;
			GUI.skin.button.wordWrap = false;

			SCANdata.SCANtype sensors = SCANcontroller.controller.activeSensorsOnVessel(vessel.id);
			SCANdata data = SCANcontroller.controller.getData(vessel.mainBody);
			data.updateImages(sensors);

			if(!repainting) { // Unity gets confused if the layout changes between layout and repaint events
				infotext = "";

				string aoff = "<color=\"grey\">";
				string aon = "<color=\"" + colorHex(c_good) + "\">";
				string abad = "<color=\"" + colorHex(c_bad) + "\">";
				string ano = "<color=\"" + colorHex(c_ugly) + "\">";
				string ac = "</color> ";
				string stat_alo = aon, stat_ahi = aon, stat_biome = aon, stat_ano = aon, stat_btdt = aon;

				minicolor = c_good;
				if(sensors == 0) minicolor = Color.grey;

				SCANcontroller.SCANsensor s;

				s = SCANcontroller.controller.getSensorStatus(vessel, SCANdata.SCANtype.AltimetryLoRes);
				if(s == null) stat_alo = aoff;
				else if(!s.inRange) stat_alo = abad;
				else if(!s.bestRange && (Time.realtimeSinceStartup % 2 < 1)) stat_alo = abad;

				s = SCANcontroller.controller.getSensorStatus(vessel, SCANdata.SCANtype.AltimetryHiRes);
				if(s == null) stat_ahi = aoff;
				else if(!s.inRange) stat_ahi = abad;
				else if(!s.bestRange && (Time.realtimeSinceStartup % 2 < 1)) stat_ahi = abad;

				s = SCANcontroller.controller.getSensorStatus(vessel, SCANdata.SCANtype.Anomaly);
				if(s == null) stat_ano = aoff;
				else if(!s.inRange) stat_ano = abad;
				else if(!s.bestRange && (Time.realtimeSinceStartup % 2 < 1)) stat_ano = abad;

				s = SCANcontroller.controller.getSensorStatus(vessel, SCANdata.SCANtype.Biome);
				if(s == null) stat_biome = aoff;
				else if(!s.inRange) stat_biome = abad;
				else if(!s.bestRange && (Time.realtimeSinceStartup % 2 < 1)) stat_biome = abad;

				s = SCANcontroller.controller.getSensorStatus(vessel, SCANdata.SCANtype.AnomalyDetail);
				if(s == null) stat_btdt = aoff;
				else if(!s.inRange) stat_btdt = abad;
				else if(!s.bestRange && (Time.realtimeSinceStartup % 2 < 1)) stat_btdt = abad;

				infotext = stat_alo + "LO" + ac + stat_ahi + "HI" + ac + stat_biome + "BIO" + ac + stat_ano + "ANOM" + ac + stat_btdt + "BTDT" + ac;

				SCANdata.SCANtype active = SCANcontroller.controller.activeSensorsOnVessel(vessel.id);
				if(active != SCANdata.SCANtype.Nothing) {
					double cov = data.getCoveragePercentage(active);
					infotext += " " + cov.ToString("N1") + "%";
					if(notMappingToday) {
						infotext = abad + "NO POWER" + ac;
					}
				} else {
					if(maptraq_frame < Time.frameCount - 5) {
						notMappingToday = true;
						infotext = abad + "NO DATA" + ac;
					}
				}

				title = "S.C.A.N. Planetary Mapping";

				if(minimode <= 0) title = " ";
			}

			Rect old_infobox = new Rect(pos_infobox);
			pos_infobox = GUILayout.Window(47110001, pos_infobox, gui_infobox_build, title, GUILayout.Width(32), GUILayout.Height(32));

			if(minimode != lastmode && pos_infobox != old_infobox) {
				snapWindow(ref pos_infobox, old_infobox);
				lastmode = minimode;
			}


			if(bigmap_visible) {
				if(bigmap == null) {
					bigmap = new SCANmap();
					bigmap.setProjection((SCANmap.MapProjection)SCANcontroller.controller.projection);
					bigmap.setWidth(SCANcontroller.controller.map_width);
					pos_bigmap.x = SCANcontroller.controller.map_x;
					pos_bigmap.y = SCANcontroller.controller.map_y;
					if(pos_bigmap.x < 0 || pos_bigmap.x >= Screen.width) pos_bigmap.x = 0;
					if(pos_bigmap.y < 0 || pos_bigmap.y >= Screen.height) pos_bigmap.y = 0;
				} else {
					SCANcontroller.controller.map_x = (int)pos_bigmap.x;
					SCANcontroller.controller.map_y = (int)pos_bigmap.y;
				}
				bigmap.setBody(vessel.mainBody);
				string rendering = "";
				if(bigmap_dragging) rendering += " ["+bigmap_drag_w+"x"+(bigmap_drag_w/2)+"]";
				if(!bigmap.isMapComplete()) rendering += " [rendering]";
				pos_bigmap = GUILayout.Window(47110002, pos_bigmap, gui_bigmap_build, "Map of " + vessel.mainBody.theName + rendering, GUILayout.Width(360), GUILayout.Height(180));
			}

			if(instruments_visible) {
				if(pos_instruments.x < 0) {
					pos_instruments.x = pos_infobox.x;
					pos_instruments.y = pos_infobox.y + pos_infobox.height + 8;
				}
				pos_instruments = GUILayout.Window(47110003, pos_instruments, gui_instruments_build, "S.C.A.N. Instruments", GUILayout.Width(200), GUILayout.Height(60));
			}

			if(settings_visible) {
				pos_settings = GUILayout.Window(47110004, pos_settings, gui_settings_build, "S.C.A.N. Settings", GUILayout.Width(360), GUILayout.Height(180));
				if(pos_settings.x < 0 && pos_settings.width > 0) {
					pos_settings.x = Screen.width/2 - pos_settings.width/2;
					pos_settings.y = Screen.height/3 - pos_settings.height/2;
					pos_settings = GUILayout.Window(47110004, pos_settings, gui_settings_build, "S.C.A.N. Settings", GUILayout.Width(360), GUILayout.Height(180));
				}
			}
		}

		public static void gui_ping_maptraq() {
			maptraq_frame = Time.frameCount;
		}

		public static void gui_ping(bool powerIsProblem) {
			notMappingToday = powerIsProblem;
			if(Time.frameCount - gui_frame_draw > 5) {
				// UI isn't working, try turning it off and on again
				RenderingManager.RemoveFromPostDrawQueue(3, guicb);
				gui_active = false;
			}
			gui_frame_ping = Time.frameCount;
			if(!gui_active) {
				RenderingManager.AddToPostDrawQueue(3, guicb);
				gui_active = true;
			}
		}
	}
}
