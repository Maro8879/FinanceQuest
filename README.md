Project Name
INVESTCOMM 

One-Line Summary
INVESTCOMM is an app that teaches people to save and invest through short lessons, quizzes and mini-games, all tailored to their goals and time horizon.

Technology Stack

Languages

C# (app and minigame logic)
JavaScript, HTML, CSS (content editor)
Objective-C++ (iOS haptics plugin)
JSON (course content)

Frameworks and libraries

Unity 6, including uGUI, TextMeshPro, Input System and Video Player
Universal Render Pipeline (2D)
Unity Test Framework (NUnit) for unit tests of the minigame logic

Platforms

Android and iOS (mobile app)
Unity Editor (development and testing)
Chrome / Edge (browser-based content editor)

APIs

Android VibrationEffect (via JNI) and iOS UIFeedbackGenerator, for haptic feedback
Browser File System Access API, so the editor can save straight into the repo
GitHub REST API, so the editor can save content directly to GitHub

Cloud services

GitHub, for the repository and collaboration
Git LFS, for images, videos and fonts

Hardware

iPhone, using the touchscreen and vibration motor
MacBook (development)
Developer PCs (Windows)

Other
Rubik font (SIL Open Font License)
Claude
ElevenLabs (For AI text-to-speech)
Pixabay

THE PROBLEM
Many people, especially young adults, never learn how to invest. Schools rarely teach it, and the information that exists is spread across books, long articles and forums full of jargon. Many people find it boring, intimidating or hard to trust. As a result, money is left on 0% accounts, people fall for "guaranteed profit" scams, or they avoid investing altogether. Financial literacy directly affects people's long-term security, so making it accessible matters.

YOUR SOLUTION
INVESTCOMM makes learning to invest easy, entertaining and accessible. It turns knowledge from books and websites into an app: short lessons in plain language with graphics, short animated videos and interactive mini-games. Each chapter ends with a test, so users check what they really understood. The app also includes simulations where users make investment decisions and immediately see the consequences, so they learn by doing without risking real money. A short onboarding survey recommends the courses that fit the user's goals, age and attitude to risk. Making tailored for people of all ages and financial capabilities.

THE WORKING PROTOTYPE
Already implemented and working:

Onboarding survey: 5 questions about experience, age, goals, risk attitude and having an investment account. It highlights recommended courses (for the prototype, recommendations are fixed).
6 courses: Investing Basics, Short / Medium / Long Horizon, Short-Term Investing and Using an Investment Platform. Each is split into chapters and levels, with unlocking and progress bars.
Lessons with text, images, tables, highlighted key facts, animated videos (with a subtitles toggle) and example product cards, e.g. savings accounts and ETFs.
Quizzes and chapter tests: multiple choice, true/false, matching pairs, drag-to-zone and fill-in-the-gap. Each has explanations, fun facts and 1–3 stars, and the best score is kept.
Mini-games:
An investment simulation, where users make decisions and watch their portfolio change.
"Truth or Trap?", where users swipe real-looking offers to spot scams.
Motivation: star counters, a unique trophy for every course, unlock animations, haptic feedback and sound effects.
Content editor: runs in the browser with a live phone preview. Non-programmers can write and edit courses and save them straight to the project or GitHub.

BEHIND THE BUILD

Mobile app: built in Unity 6 with C#. The whole UI (uGUI + TextMeshPro) is generated in code, which keeps layouts consistent and easy to change.
Content: all course content lives in JSON files, one per chapter, next to its images and videos. An editor importer packs it into a single content database, so adding a lesson needs no code.
Content editor: a single HTML/JavaScript file. It saves through the browser File System Access API or the GitHub API, includes a 3-way merge so teammates don't overwrite each other, and warns about missing answers or images.
Graphics: illustrations, trophies, charts and icons are drawn procedurally (signed-distance shapes) and cached. This keeps the app small and sharp on every screen.
Mini-game logic: lives in a separate engine-independent assembly covered by unit tests (Unity Test Framework / NUnit).
Performance: decisions were made for average phones: cached sprites, mesh-drawn chart lines instead of textures, minimal masks, and animations that only move elements instead of rebuilding them.
Native features: haptics through Android VibrationEffect and an iOS plugin.

CHALLENGES AND DISCOVERIES

Team collaboration: at one point a shared code package overwrote a teammate's work. We moved to syncing individual files through Git and added merge support to the editor. We learned to treat content and code as separate workflows.
Content creation for non-programmers: editing JSON by hand was error-prone, so we built a visual editor with validation and a live preview.
Mobile UX details: cards were opening while users scrolled, so we raised the drag threshold and cancel taps once a swipe starts. Product cards made as images couldn't be clicked, so we replaced them with native, editable cards.
Design consistency: we iterated on icons, trophies and layout many times, comparing visual previews before implementing.

RESULTS AND NEXT STEPS

Result: a working prototype with 6 courses, interactive lessons, 5 quiz types, 2 mini-games, an onboarding survey and a content editor the whole team uses.
How we tested it: in the Unity Editor and on iPhone, with unit tests for the mini-game logic and regular team reviews.
Most proud of: [np. that non-programmers on the team can create full lessons themselves, or the "Truth or Trap?" scam-spotting game].
Current limitations:
Survey recommendations are fixed for the prototype.
Progress is stored only on the device.
Some courses still have little content.
Partner links ("SEE MORE") are not connected yet.
Next steps:
Personalised recommendations from the survey.
More lessons and simulations.
User accounts with cloud sync.
Android release.
Testing with real users.
Develop "Using the investment platform" course basing on real commerzbank investment platform.

