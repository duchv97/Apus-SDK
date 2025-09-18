using AppsFlyerSDK;
using Firebase.Analytics;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Transactions;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Security;

public class IAPManager : MonoBehaviour, IStoreListener
{
    [SerializeField] private bool _useFakeStore;

    private static IStoreController m_StoreController;
    private static IExtensionProvider m_StoreExtensionProvider;

    public List<IAPInfoSO> ListSubscriptionActive = new List<IAPInfoSO>();

    public static IAPManager I;

    //public static Action OnBuyFailed;

    //public bool IsBuyPopupShowed { get; set; }

    private void Awake()
    {
        I = this;
    }

    private void Start()
    {
        if (m_StoreController == null)
        {
            InitializePurchasing();
        }
    }

    private void InitializePurchasing()
    {
        // If we have already connected to Purchasing ...
        if (IsInitialized())
        {
            // ... we are done here.
            return;
        }

        if (_useFakeStore)
        {
            StandardPurchasingModule.Instance().useFakeStoreUIMode = FakeStoreUIMode.StandardUser;
            StandardPurchasingModule.Instance().useFakeStoreAlways = true;
        }

        // Create a builder, first passing in a suite of Unity provided stores.
        var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());

        for (int i = 0; i < IAPConfig.I.IAPInfoSOs.Length; i++)
        {
            builder.AddProduct(IAPConfig.I.IAPInfoSOs[i].GetProductID(), IAPConfig.I.IAPInfoSOs[i].ProductType);
        }

        // Kick off the remainder of the set-up with an asynchrounous call, passing the configuration 
        UnityPurchasing.Initialize(this, builder);
    }


    private bool IsInitialized()
    {
        // Only say we are initialized if both the Purchasing references are set.
        return m_StoreController != null && m_StoreExtensionProvider != null;
    }

    public void Buy(IAPInfoSO info)
    {
        //IsBuyPopupShowed = true;
        AdsManager.Instance.IgnoreShowAOAWhenBackToGame = true;

        BuyProductID(info.GetProductID());

        var customFields = new Dictionary<string, object> { { "Pack_Name", info.GetProductID() } };
        //Parameter levelParam = new Parameter("Level", DataManager.Instance.CurrentLevel.ToString());
        AnalyticsManager.Instance.LogEvent("IAP_Buy", customFields);//, levelParam);
    }

    void BuyProductID(string productId)
    {
        // If Purchasing has been initialized ...
        if (IsInitialized())
        {
            // ... look up the Product reference with the general product identifier and the Purchasing 
            // system's products collection.
            Product product = m_StoreController.products.WithID(productId);

            // If the look up found a product for this device's store and that product is ready to be sold ... 
            if (product != null && product.availableToPurchase)
            {
                Debug.Log(string.Format("Purchasing product asychronously: '{0}'", product.definition.id));
                // ... buy the product. Expect a response either through ProcessPurchase or OnPurchaseFailed 
                // asynchronously.
                m_StoreController.InitiatePurchase(product);
            }
            // Otherwise ...
            else
            {
                // ... report the product look-up failure situation  
                Debug.Log("BuyProductID: FAIL. Not purchasing product, either is not found or is not available for purchase");
            }
        }
        // Otherwise ...
        else
        {
            // ... report the fact Purchasing has not succeeded initializing yet. Consider waiting longer or 
            // retrying initiailization.
            Debug.Log("BuyProductID FAIL. Not initialized.");
        }
    }


    // Restore purchases previously made by this customer. Some platforms automatically restore purchases, like Google. 
    // Apple currently requires explicit purchase restoration for IAP, conditionally displaying a password prompt.
    public void RestorePurchases()
    {
        // If Purchasing has not yet been set up ...
        if (!IsInitialized())
        {
            // ... report the situation and stop restoring. Consider either waiting longer, or retrying initialization.
            Debug.Log("RestorePurchases FAIL. Not initialized.");
            return;
        }

        // If we are running on an Apple device ... 
        if (Application.platform == RuntimePlatform.IPhonePlayer ||
            Application.platform == RuntimePlatform.OSXPlayer)
        {
            // ... begin restoring purchases
            Debug.Log("RestorePurchases started ...");

            // Fetch the Apple store-specific subsystem.
            var apple = m_StoreExtensionProvider.GetExtension<IAppleExtensions>();
            // Begin the asynchronous process of restoring purchases. Expect a confirmation response in 
            // the Action<bool> below, and ProcessPurchase if there are previously purchased products to restore.
            apple.RestoreTransactions((result) =>
            {
                // The first phase of restoration. If no more responses are received on ProcessPurchase then 
                // no purchases are available to be restored.
                Debug.Log("RestorePurchases continuing: " + result + ". If no further messages, no purchases available to restore.");
            });
        }
        // Otherwise ...
        else
        {
            // We are not running on an Apple device. No work is necessary to restore purchases.
            Debug.Log("RestorePurchases FAIL. Not supported on this platform. Current = " + Application.platform);
        }
    }


    //  
    // --- IStoreListener
    //

    public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
    {
        // Purchasing has succeeded initializing. Collect our Purchasing references.
        Debug.Log("OnInitialized: PASS");

        // Overall Purchasing system, configured with products for this application.
        m_StoreController = controller;
        // Store specific subsystem, for accessing device-specific store features.
        m_StoreExtensionProvider = extensions;

        if (!_useFakeStore)
            IAPConfig.I.UpdateData(controller.products);

        CheckSubscription();
    }


    public void OnInitializeFailed(InitializationFailureReason error)
    {
        // Purchasing set-up has not succeeded. Check error for reason. Consider sharing this reason with the user.
        Debug.Log("OnInitializeFailed InitializationFailureReason:" + error);
    }

    public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
    {
        bool validPurchase = true; // Presume valid for platforms with no R.V.

        // Unity IAP's validation logic is only included on these platforms.
#if (UNITY_ANDROID || UNITY_IOS || UNITY_STANDALONE_OSX)&&!UNITY_EDITOR
        // Prepare the validator with the secrets we prepared in the Editor
        // obfuscation window.
        var validator = new CrossPlatformValidator(GooglePlayTangle.Data(),
            AppleTangle.Data(), Application.identifier);

        try
        {
            // On Google Play, result has a single product ID.
            // On Apple stores, receipts contain multiple products.
            var result = validator.Validate(args.purchasedProduct.receipt);
            // For informational purposes, we list the receipt(s)
            Debug.Log("Receipt is valid. Contents:");
            foreach (IPurchaseReceipt productReceipt in result)
            {
                Debug.Log(productReceipt.productID);
                Debug.Log(productReceipt.purchaseDate);
                Debug.Log(productReceipt.transactionID);
            }
        }
        catch (IAPSecurityException)
        {
            Debug.Log("Invalid receipt, not unlocking content");
            validPurchase = false;
        }
#endif

        if (_useFakeStore || validPurchase)
        {
            // Unlock the appropriate content here.
            bool checkDone = false;
            for (int i = 0; i < IAPConfig.I.IAPInfoSOs.Length; i++)
            {
                if (String.Equals(args.purchasedProduct.definition.id, IAPConfig.I.IAPInfoSOs[i].GetProductID(), StringComparison.Ordinal))
                {
                    Debug.Log(string.Format("ProcessPurchase: PASS. Product: '{0}'", args.purchasedProduct.definition.id));
                    // The consumable item has been successfully purchased, add 100 coins to the player's in-game score.
                    IAPConfig.I.IAPInfoSOs[i].BuySuccess();

                    //HiGame.IapAppsFlyerAnalyticsObj.Instance.ProcessPurchase(args);
                    //AnalyticsManager.Instance.LogIAPEvent(args);

                    checkDone = true;
                    break;
                }
            }
            if (checkDone == false)
            {
                Debug.Log(string.Format("ProcessPurchase: FAIL. Unrecognized product: '{0}'", args.purchasedProduct.definition.id));
            }
            else CheckSubscription();

        }
        // Return a flag indicating whether this product has completely been received, or if the application needs 
        // to be reminded of this purchase at next app launch. Use PurchaseProcessingResult.Pending when still 
        // saving purchased products to the cloud, and when that save is delayed. 
        return PurchaseProcessingResult.Complete;
    }


    public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
    {
        // A product purchase attempt did not succeed. Check failureReason for more detail. Consider sharing 
        // this reason with the user to guide their troubleshooting actions.
        Debug.Log(string.Format("OnPurchaseFailed: FAIL. Product: '{0}', PurchaseFailureReason: {1}", product.definition.storeSpecificId, failureReason));
        //OnBuyFailed?.Invoke();
    }

    private bool IsSubscribedTo(Product subscription)
    {
        // If the product doesn't have a receipt, then it wasn't purchased and the user is therefore not subscribed.
        if (subscription.receipt == null)
        {
            return false;
        }

        //The intro_json parameter is optional and is only used for the App Store to get introductory information.
        var subscriptionManager = new SubscriptionManager(subscription, null);

        // The SubscriptionInfo contains all of the information about the subscription.
        // Find out more: https://docs.unity3d.com/Packages/com.unity.purchasing@3.1/manual/UnityIAPSubscriptionProducts.html
        var info = subscriptionManager.getSubscriptionInfo();

        return info.isSubscribed() == Result.True;
    }

    private void CheckSubscription()
    {
        for (int i = 0; i < IAPConfig.I.IAPInfoSOs.Length; i++)
        {
            if (IAPConfig.I.IAPInfoSOs[i].ProductType != ProductType.Subscription) return;

            var subscriptionProduct = m_StoreController.products.WithID(IAPConfig.I.IAPInfoSOs[i].GetProductID());

            try
            {
                var isSubscribed = IsSubscribedTo(subscriptionProduct);
                ListSubscriptionActive.Add(IAPConfig.I.IAPInfoSOs[i]);
                // isSubscribedText.text = isSubscribed ? "You are subscribed" : "You are not subscribed";
            }
            catch (StoreSubscriptionInfoNotSupportedException)
            {
                var receipt = JsonUtility.FromJson<Dictionary<string, object>>(subscriptionProduct.receipt);
                var store = receipt["Store"];
                //isSubscribedText.text =
                //    "Couldn't retrieve subscription information because your current store is not supported.\n" +
                //    $"Your store: \"{store}\"\n\n" +
                //    "You must use the App Store, Google Play Store or Amazon Store to be able to retrieve subscription information.\n\n" +
                //    "For more information, see README.md";
            }
        }
    }

    public void OnInitializeFailed(InitializationFailureReason error, string message)
    {
        Debug.Log("OnInitializeFailed InitializationFailureReason:" + error);
    }
}

