
// value of Platform: SupportedPlatforms
// value of AndroidSdkVersion: ANDROID_SDK_VERSION
// value of UseGsmLocator: USE_GSM_LOCATOR

#if( Platform == android )
    var androidSdkVer = "ANDROID_SDK_VERSION";
#endif

#if( IsMobile )
    var useGsmLocator = USE_GSM_LOCATOR;
#endif

#if( IsAndroidOnly )
    // Some android only specific code
#endif