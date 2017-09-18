using UnityEngine;
using System.Collections;
using System;

public class NoodlePermissionGranter : MonoBehaviour
{

    // subscribe to this callback to see if your permission was granted.
    public static Action<bool> PermissionRequestCallback;

    // for now, it only implements the external storage permission
    public enum NoodleAndroidPermission
    {
        WRITE_EXTERNAL_STORAGE
    }
    public static void GrantPermission(NoodleAndroidPermission permission)
    {
        if (!initialized)
            initialize();

        noodlePermissionGranterClass.CallStatic("grantPermission", activity, (int)permission);
    }







    //////////////////////////////
    /// Initialization Stuff /////
    //////////////////////////////

    // it's a singleton, but no one needs to know about it. hush hush. dont touch me.
    private static NoodlePermissionGranter instance;
    private static bool initialized = false;

    public void Awake()
    {
        // instance is also set in initialize.
        // having it here ensures this thing doesnt break
        // if you added this component to the scene manually
        instance = this;
        DontDestroyOnLoad(this.gameObject);
        // object name must match UnitySendMessage call in NoodlePermissionGranter.java
        if (name != NOODLE_PERMISSION_GRANTER)
            name = NOODLE_PERMISSION_GRANTER;
    }


    private static void initialize()
    {
        // runs once when you call GrantPermission

        // add object to scene
        if (instance == null)
        {
            GameObject go = new GameObject();
            // instance will also be set in awake, but having it here as well seems extra safe
            instance = go.AddComponent<NoodlePermissionGranter>();
            // object name must match UnitySendMessage call in NoodlePermissionGranter.java
            go.name = NOODLE_PERMISSION_GRANTER;
        }

        // get the jni stuff. we need the activty class and the NoodlePermissionGranter class.
        noodlePermissionGranterClass = new AndroidJavaClass("com.noodlecake.unityplugins.NoodlePermissionGranter");
        AndroidJavaClass u3d = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        activity = u3d.GetStatic<AndroidJavaObject>("currentActivity");

        initialized = true;
    }







    ///////////////////
    //// JNI Stuff ////
    ///////////////////

    static AndroidJavaClass noodlePermissionGranterClass;
    static AndroidJavaObject activity;
    private const string WRITE_EXTERNAL_STORAGE = "WRITE_EXTERNAL_STORAGE";
    private const string PERMISSION_GRANTED = "PERMISSION_GRANTED"; // must match NoodlePermissionGranter.java
    private const string PERMISSION_DENIED = "PERMISSION_DENIED"; // must match NoodlePermissionGranter.java
    private const string NOODLE_PERMISSION_GRANTER = "NoodlePermissionGranter"; // must match UnitySendMessage call in NoodlePermissionGranter.java

    private void permissionRequestCallbackInternal(string message)
    {
        // were calling this method from the java side.
        // the method name and gameobject must match NoodlePermissionGranter.java's UnitySendMessage
        bool permissionGranted = (message == PERMISSION_GRANTED);
        if (PermissionRequestCallback != null)
            PermissionRequestCallback(permissionGranted);
    }
}
NoodlePermissionGranter.java:

package com.noodlecake.unityplugins;


///////////////////////////////////////////////////////////
///////////////// NoodlePermissionGranter /////////////////
/// Implements runtime granting of Android permissions. ///
/// This is necessary for Android M (6.0) and above. //////
///////////////////////////////////////////////////////////   
//////////////////// Noodlecake Studios ///////////////////
///////////////////////////////////////////////////////////

import android.Manifest;
import android.os.Build;
import android.app.Activity;
import android.app.Fragment;
import android.app.FragmentManager;
import android.app.FragmentTransaction;
import android.util.Log;
import android.content.pm.PackageManager;
import java.io.File;
import com.unity3d.player.UnityPlayerActivity;
import com.unity3d.player.UnityPlayer;

public class NoodlePermissionGranter
{
    // Only implements WRITE_EXTERNAL_STORAGE so far.
    // Implement the rest by matching the enum in NoodlePermissionGranter.cs
    // to the getPermissionStringFromEnumInt below.

    private final static String UNITY_CALLBACK_GAMEOBJECT_NAME = "NoodlePermissionGranter";
    private final static String UNITY_CALLBACK_METHOD_NAME = "permissionRequestCallbackInternal";
    private final static String PERMISSION_GRANTED = "PERMISSION_GRANTED"; // this will be an arg to the above method
    private final static String PERMISSION_DENIED = "PERMISSION_DENIED";

    public static String getPermissionStringFromEnumInt(int permissionEnum) throws Exception
    {
        switch (permissionEnum)
        {
            case 0:
                return Manifest.permission.WRITE_EXTERNAL_STORAGE;
            // "and the rest is still unwritten" - Natasha Bedingfield
        }
        Log.e("NoodlePermissionGranter", "Error. Unknown permissionEnum " + permissionEnum);
        throw new Exception(String.format("Error. Unknown permissionEnum %d", permissionEnum));
    }

public static void grantPermission(Activity currentActivity, int permissionEnum)
{
    // permission enum must match ordering in NoodlePermissionGranter.cs
    final Activity act = currentActivity;
    Log.i("NoodlePermissionGranter", "grantPermission " + permissionEnum);
    if (Build.VERSION.SDK_INT < 23)
    {
        Log.i("NoodlePermissionGranter", "Build.VERSION.SDK_INT < 23 (" + Build.VERSION.SDK_INT + ")");
        UnityPlayer.UnitySendMessage(UNITY_CALLBACK_GAMEOBJECT_NAME, UNITY_CALLBACK_METHOD_NAME, PERMISSION_GRANTED);
        return;
    }

    try
    {
        final int PERMISSIONS_REQUEST_CODE = permissionEnum;
        final String permissionFromEnumInt = getPermissionStringFromEnumInt(permissionEnum);
        if (currentActivity.checkCallingOrSelfPermission(permissionFromEnumInt) == PackageManager.PERMISSION_GRANTED)
        {
            Log.i("NoodlePermissionGranter", "already granted");
            UnityPlayer.UnitySendMessage(UNITY_CALLBACK_GAMEOBJECT_NAME, UNITY_CALLBACK_METHOD_NAME, PERMISSION_GRANTED);
            return;
        }

        final FragmentManager fragmentManager = currentActivity.getFragmentManager();
        final Fragment request = new Fragment() {

                @Override public void onStart()
{
    super.onStart();
    Log.i("NoodlePermissionGranter", "fragment start");
    String[] permissionsToRequest = new String[] { permissionFromEnumInt };
    Log.i("NoodlePermissionGranter", "fragment start " + permissionsToRequest[0]);
    requestPermissions(permissionsToRequest, PERMISSIONS_REQUEST_CODE);
}

@Override public void onRequestPermissionsResult(int requestCode, String[] permissions, int[] grantResults)
{
    Log.i("NoodlePermissionGranter", "onRequestPermissionsResult");
    if (requestCode != PERMISSIONS_REQUEST_CODE)
        return;

    if (grantResults.length > 0
        && grantResults[0] == PackageManager.PERMISSION_GRANTED)
    {
        // permission was granted, yay! Do the
        // contacts-related task you need to do.
        Log.i("NoodlePermissionGranter", PERMISSION_GRANTED);
        UnityPlayer.UnitySendMessage(UNITY_CALLBACK_GAMEOBJECT_NAME, UNITY_CALLBACK_METHOD_NAME, PERMISSION_GRANTED);
    }
    else
    {

        // permission denied, boo! Disable the
        // functionality that depends on this permission.
        Log.i("NoodlePermissionGranter", PERMISSION_DENIED);
        UnityPlayer.UnitySendMessage(UNITY_CALLBACK_GAMEOBJECT_NAME, UNITY_CALLBACK_METHOD_NAME, PERMISSION_DENIED);
    }


    FragmentTransaction fragmentTransaction = fragmentManager.beginTransaction();
    fragmentTransaction.remove(this);
    fragmentTransaction.commit();

    // shouldBeOkayToStartTheApplicationNow();
}
            };

            FragmentTransaction fragmentTransaction = fragmentManager.beginTransaction();
fragmentTransaction.add(0, request);
            fragmentTransaction.commit();
        }
        catch(Exception error)
        {
            Log.w("[NoodlePermissionGranter]", String.format("Unable to request permission: %s", error.getMessage()));
            UnityPlayer.UnitySendMessage(UNITY_CALLBACK_GAMEOBJECT_NAME, UNITY_CALLBACK_METHOD_NAME, PERMISSION_DENIED);
        }
    }

}