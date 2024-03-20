# AOEffect3D
 This project has been developed for the Hack The Weave hackathon organized by Weavers. The bounty is to build a Graphical User Interface for ao-Effect, the first game developed on AO.
 ao-Effect is a game where each player designs their own bot to compete in an arena against others. The bots can move or attack and have a set amount of health.
 I've developed the project with Unity3d because it was for me the best way to have a multi-platform and 3D approach to it. 
 In the repository you can find all the Unity project source code and one with the custom Lua blueprints I've developed for this project.
 
 Here is a [folder](https://drive.google.com/drive/folders/1zsY9SOOj8xl0AcrZ_pRshSw9njkCCr5-?usp=sharing) with submission video, a zip file with Windows build and the Apk Android build
 
 The Windows build is the most complete: you can create a process, customize and load blueprints and play with your bot. While the Android app is a viewer in which you can watch what happens in a ao-Effect game istance.
 Potentially, thanks to Unity, I can easily build also versions on MacOS, Linux, iOS and WebGL, but I couldn't because I didn't have the possibility to test them since I don't have Mac or Linux devices.
 
 ![](https://github.com/simoamico94/AOEffect3D/assets/17854691/68821ffa-a6a1-4ccb-a97d-bb0260b7644e)

 # Requirements
 To install and use the Android version you just need to enable developer mode and install the .apk file as you would do with any other custom .apk loading. You also need an internet connection.
 For the Windows build you need an internet connection too and AOS (AO operative system) installed on the same disk you are loading the app. For guides on how and what is required to install AOS and how it works, you can follow the main documentation [here](https://cookbook_ao.g8way.io/welcome/getting-started.html).
 
 # Guide
 Here I wrote a guide to understand how to play the game and its main features. Here you can find the [video guide](https://drive.google.com/file/d/1hzPW_a1b0Vmthobt7chmYIzWjY3FyrnM/view?usp=sharing). 
 
 ## Login Page
 ![Login page](https://github.com/simoamico94/AOEffect3D/assets/17854691/441658f7-5673-4cc7-8017-a98337f2505c)
 
 When you first open the app you will see te login page. From here you can choose a name, a custom ARWeave wallet (if empty will be created a new default system wallet) and some blueprints to load. The combination name-wallet let you create a unique new process and login back to it later.
 Blueprints are .lua files with which you can add logic to your process. As you can read from the hints, blueprints to load need to be located in the AOEffect_Data/StreamingAsset folder, inside the build folder. You can find some pre-loaded blueprints downloaded from the [cookbook](https://cookbook_ao.g8way.io/guides/aos/blueprints/index.html).
 Here you can also customize and create your own variants of 3 blueprints: Bot, Arena and AOEffect. Bot is the one needed if you want to compete in a ao-Effect game, while the other two are needed (with also the Token blueprint) in order to create your custom arena.
 
 ![Customization example](https://github.com/simoamico94/AOEffect3D/assets/17854691/821061d5-0446-4dde-8128-5b01b8519a1d)
 
 Here is an example on how you can customize a blueprint, changing some of the parameter to make it unique!

 ## Console
 When you click on login, underneath will be spawned a cmd thread that can be used through the console panel. If you are a more advanced user you can use it to send messages to other processes and read input.

 ![Console](https://github.com/simoamico94/AOEffect3D/assets/17854691/2b78c633-4965-48da-940d-2ba03dfdaa58)

 ## Game Intro
 ![image](https://github.com/simoamico94/AOEffect3D/assets/17854691/368e9f06-61e1-4217-8ce1-cb5bdaaecfd8)
 
 After login you will see the Game Intro panel. When you open the Android app, which as explained before has only the game view part, you will start from here. From the top you can toggle the console (only on Windows) and logout.
 Here you can choose which arena to join: you can type the unique arena process ID or you can choose from all the arena registered to the arena manager process that I've built. In the _CustomLuaBlueprints folder you can find how is created.
 In order to register you custom arena you need first to create one. Here is the procedure:
 - Create your custom versions of arena and aoeffect lua scripts as described before
 - From the main login page you can optionally choose a name and/or connect a wallet. Then, select the blueprints to load: one for the custom arena, one for the custom ao-effect and also import token.lua bluprint
 - After login, open the console and type: Send({ Target = "I0sKrk8f7uirSaPaMzf3NmhxAwmaXJFoDYuwYGZl7II", Action = "Register"}) 
 - If you want to unregister it, type: Send({ Target = "I0sKrk8f7uirSaPaMzf3NmhxAwmaXJFoDYuwYGZl7II", Action = "Unregister"}) you will unregister it
 
 Once you register an arena, it will be added in this panel and everyone will see it and can load it with just a click!

 ![Available Arenas](https://github.com/simoamico94/AOEffect3D/assets/17854691/af4bf922-7fb1-4285-96a8-0db0bd34dda6)

 ## Waiting View 
 ![image](https://github.com/simoamico94/AOEffect3D/assets/17854691/a9ce2468-3d9a-4429-97ff-e7130793f217)

 Now we are finally ready to play! Each game has two phases: Waiting and Playing. 
 For both you will see on the top how much time they will last. While in waiting mode, you will see a list of all the processes registered to arena. They are red if they haven't already pay, green if they paid and are ready to play.
 If your process is not ready to play (not appearing in the list or is red) you will see a "Register" button. Once clicked, all the registration flow will be automatically handled: registration, token request and sending tokens to the arena. If everything succeeds you will see that your process first will appear in red and then becomes green.
 You can check which is your process name from the bottom right corner after login.

 ![image](https://github.com/simoamico94/AOEffect3D/assets/17854691/6d75d568-961d-4318-a173-2eca45c4fe74)

 When the minimum amount of paid players is reached and the countdown reaches 0, the game will automatically start.
 
 ## Game View
 ![image](https://github.com/simoamico94/AOEffect3D/assets/17854691/e716e86a-8127-40ca-97ec-c799a57e060f)

 Once the game starts you can fly around with the camera, using screen joysticks on Android and in Windows you can move with WASD or arrow and rotate with right mouse click and pan. You will see a big vertical purple ray on top of the bot which represents your local one.
 Each Bot also has on top a panel giving information about its process name, ranking, health and energy.
 
 ![image](https://github.com/simoamico94/AOEffect3D/assets/17854691/e131d600-38c1-4e37-967b-6d3e73a79bbe)

 If you are playing with your local player you can open a Move pad panel with buttons to send commands to your bot: move in each direction or try a max energy attack. Will you be stronger than bots?
 When a bot attacks, you will see a shooting animation with sound (see video for details).
 
 ![image](https://github.com/simoamico94/AOEffect3D/assets/17854691/b424496e-8b65-4504-a7a2-ba81e485cc40)

 On the right side you can find the leaderboard button. From here you can see the leaderboard and if you want to focus on a precise bot you can click on one of the entries to move the camera behind him.
 When time elapses or there will be one only one player alive, the game will finish and return back to the Waiting view, in order to prepare the next round.

 # Blueprint Customization
 In this guide I've described the flow when using the enhanced versions of arena.lua and aoeffect.lua blueprints that I've made. Everything will work also with the base versions that you can view on the [cookbook](https://cookbook_ao.g8way.io/tutorials/bots-and-games/arena-mechanics.html), but it will be limited in some parts.
 More precisely:
 - I've added Waiting players info in the arena Game State
 - I've created some data structures to store the list of attacks happening and relative handler to get them
 - I've created an ao-Effect info data structure with all the parameters of the game and relative handler to get them
 

 

 

 
 
