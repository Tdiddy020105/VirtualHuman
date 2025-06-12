This Unity project serves as the front-end for the Peter Dielesen Virtual Human, 
part of the Human Zoo research installation at Fontys ICT. It connects to a Python Flask 
server to enable live emotional conversation with a digitally reconstructed ancestor

Requirements:
-Unity version 6000.0.45f1
-FMOD Unity Integration (included in Assets/Plugins)
-Flask server running locally at http://localhost:5000 (see back-end README)

Step 1:
FMOD is already included in the repo. If Unity throws errors:
-Open the FMOD Studio Settings (menu: FMOD → Edit Settings)
-Set the Build Path to your local bank directory (optional if not using custom sounds)

Step 2:
Use Unity Hub to open the project

Step 3:
Make sure the back-end flask server is running

Step 4:
Start in IntroScene. Press "Start Simualtie" to begin.

Developer Tips:
-Subtitles are tied to TTS output — synced in TTSManager.cs
-FMOD handles raw MP3 streaming — no WAV conversion needed
-To swap voices or languages, update ElevenLabs settings on the Flask server
-The orb’s color reflects Peter’s emotional state (based on AI-tagged emotion)

Support:
For questions or collaboration, contact Thomas Dielesen
thomas@dielesen.eu