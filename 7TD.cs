using System;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Logging;
using UnityEngine;

namespace SevenTD
{
    using P = Plugin;

    [BepInProcess("Legion TD 2.exe")]
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        internal new static ManualLogSource Logger;

        private string _chatViewsFileAbs;
        private string _chatViewsBackupFileAbs;

        // When the plugin is loaded
        public void Awake() {
            // Create masking Logger as internal to use more easily in code
            Logger = base.Logger;

            // Get paths to game js folder and to our future modded gateway
            _chatViewsFileAbs =
                Path.Combine(Paths.GameRootPath, "Legion TD 2_Data", "uiresources", "AeonGT", "hud", "js", "chat-views.js");
            _chatViewsBackupFileAbs =
                Path.Combine(Paths.GameRootPath, "Legion TD 2_Data", "uiresources", "AeonGT", "hud", "js", "chat-views-backup.js");

            // Inject custom js and patch c#
            try {
                CleanUp();
                ReplaceChatViews();
            }
            catch (Exception e) {
                Logger.LogError($"Error while injecting or patching: {e}");
                throw;
            }

            Application.quitting += OnApplicationQuit; // register quit event handler to cleanup the nasty stuff whenever the game is closed. 
            
            // All done!
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_NAME} is loaded!");
        }

        // Unpatch if plugin is destroyed to handle in-game plugin reloads
        // Remove files we created
        public void OnApplicationQuit() {
            Logger.LogInfo("Cleaning up...");
            CleanUp();
        }

        // Adds content of embedded html to the original gateway
        // Save result in custom gateway that we'll force the game to use
        private void ReplaceChatViews() {
            var lines = File.ReadAllLines(_chatViewsFileAbs); // get the content of the current chatviews

            // check if file is already modded
            if (!!lines.Any<string>((l) => l.Contains("//7TD"))) {
                Logger.LogError("7TD: Chatviews already modded :O, skipping injecting.");
                return;
            }
            if (lines.Length != 1049) {
                Logger.LogError("7TD: Skipping injection, file is not expected length of 1049 but was " + lines.Length);
                return;
            }
            if (File.Exists(_chatViewsBackupFileAbs)) { File.Delete(_chatViewsBackupFileAbs); } // remove the backup if it exists, we're making a new one
            File.Copy(_chatViewsFileAbs, _chatViewsBackupFileAbs); // making a new backup

            lines[28] = lines[28] + @$"
            sevenTv: {{}},
            previewFilter: """" //7TD fam";

            lines[121] = lines[121] + $@"
getRequest('https://ltd2.krettur.no/v2/emotes', function (error, body) {{
            if (error) return
            var temp = {{}}
            try {{
                var data = JSON.parse(body)
                Object.keys(data).map(function (name) {{
                    temp["":"" + name + "":""] = data[name] // adding the : to ease the indexing later, xdd
                }})
                parent.setState({{ sevenTv: temp }})
            }}
            catch (e) {{
                console.log('7TV ERROR:', e)
            }}

        }})";

            lines[290] = lines[290] + $@"{{";
            lines[292] = $@"
this.setState({{previewFilter: event.target.value.substr(index + 1) }})
}}
" + lines[292] + $@"{{";
            lines[293] = lines[293] + $@"
this.setState({{ previewFilter: """" }})
            }}";

            lines[587] = "__html: parseSevenTVEmotes(line, parent.state.sevenTv).content";
            lines[595] = lines[595].Replace("})", "") + $@", emotes: parent.state.sevenTv, previewFilter: parent.state.previewFilter }})";
            lines[631] = lines[631] + $@"
                                emotes: this.state.sevenTv";
            lines[636] = lines[636].Replace("})", "") + $@", emotes: this.state.sevenTv, previewFilter: this.state.previewFilter }})";

            lines[681] = "//" + lines[681];
            lines[682] = "//" + lines[682];
            lines[683] = "//" + lines[683];
            lines[695] = lines[695] + $@"
            if (parent.props.previewFilter != """") {{
                var found = 0
                Object.keys(parent.props.emotes).filter(function (emote) {{
                    if (found >= 4) {{ return false }}
                    if (emote.toLowerCase().indexOf(parent.props.previewFilter.toLowerCase()) !== -1) {{
                        found ++
                        return true
                    }}
                    return false 
                }}).map(function (emote, index) {{
                    if (index <= 3) {{
                        previewList.push({{
                            icon: parent.props.emotes[emote],
                            name: emote,
                            originalName: emote
                        }})
                    }}
                    index++
                }})
            }}
";
            lines[773] = lines[773] + $@"
        emotes: React.PropTypes.Object";

            lines[792] = lines[792].Replace("line", "parseSevenTVEmotes(line, parent.props.emotes)");
            lines[1048] = lines[1048] + $@"

function parseSevenTVEmotes(html, emotes) {{
    var re = new RegExp(/:[A-z]*:/g)
    var ma
    try {{
        while ((ma = re.exec(html.content)) !== null) {{
            if (!!emotes && emotes.hasOwnProperty(ma[0])) {{
                html.content = html.content.replace(ma[0], '<img class=""emoji-icon-big"" src=""' + emotes[ma[0]] + '""/>')
            }}
        }}
    }}
    catch (e) {{
        console.log(""7TV: found ERROR in regexing!"", e)
        return html
    }}
    return html
}}

function getRequest(url, callback) {{

    var xhr = new XMLHttpRequest();

    xhr.open('GET', url, true);

    xhr.onload = function () {{
        if (xhr.status === 200) {{
            callback(null, xhr.responseText);
        }} else {{
            callback('Error: ' + xhr.status);
        }}

    }}
    xhr.send();
}}";

            File.WriteAllLines(_chatViewsFileAbs, lines);

            Logger.LogInfo("7TD: Injected emotes in the chat-views.js");
        }

        // Delete custom gateway file
        private void CleanUp() {
            if (File.Exists(_chatViewsBackupFileAbs)) {
                // todo enable dis badaboio
                File.Delete(_chatViewsFileAbs); // delete the modified version
                File.Move(_chatViewsBackupFileAbs, _chatViewsFileAbs); // put the original back
            }
        }
    }
}
