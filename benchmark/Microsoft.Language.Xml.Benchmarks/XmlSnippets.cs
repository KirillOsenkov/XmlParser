using System;

namespace Microsoft.Language.Xml.Benchmarks
{
    public static class XmlSnippets
    {
        public const string LongAndroidLayoutXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<moyeu.InfoPane xmlns:android=""http://schemas.android.com/apk/res/android""
    android:id=""@+id/infoPane""
    android:orientation=""vertical""
    android:layout_height=""wrap_content""
    android:layout_width=""match_parent""
    android:layout_gravity=""bottom""
    android:paddingTop=""4dp"">
    <FrameLayout
        android:layout_height=""72dp""
        android:layout_width=""match_parent""
        android:paddingEnd=""16dp""
        android:paddingStart=""16dp""
        android:background=""@color/card_background""
        android:id=""@+id/PaneHeaderView"">
        <LinearLayout
            android:orientation=""vertical""
            android:layout_height=""wrap_content""
            android:layout_width=""match_parent""
            android:layout_gravity=""center"">
            <LinearLayout
                android:orientation=""horizontal""
                android:layout_height=""wrap_content""
                android:layout_width=""match_parent""
                android:paddingRight=""72dp"">
                <TextView
                    android:id=""@+id/InfoViewName""
                    android:text=""Union Square""
                    android:layout_width=""0px""
                    android:layout_weight=""1""
                    android:layout_height=""wrap_content""
                    android:textColor=""?android:attr/textColorPrimary""
                    android:textAppearance=""@style/TextAppearance.AppCompat.Title""
                    android:lines=""1""
                    android:ellipsize=""end"" />
                <TextView
                    android:text=""8""
                    android:layout_width=""wrap_content""
                    android:layout_height=""wrap_content""
                    android:gravity=""right|center_vertical""
                    android:minWidth=""32dp""
                    android:id=""@+id/InfoViewBikeNumber""
                    android:textColor=""#000000""
                    android:textSize=""18sp""
                    android:textStyle=""bold"" />
                <ImageView
                    android:layout_width=""wrap_content""
                    android:layout_height=""wrap_content""
                    android:layout_marginLeft=""4dp""
                    android:src=""@android:color/transparent""
                    android:id=""@+id/InfoViewBikeNumberImg""
                    android:baseline=""15dp"" />
            </LinearLayout>
            <LinearLayout
                android:orientation=""horizontal""
                android:layout_height=""wrap_content""
                android:layout_width=""match_parent""
                android:baselineAligned=""true"">
                <TextView
                    android:id=""@+id/InfoViewSecondName""
                    android:text=""Somerville""
                    android:layout_width=""0px""
                    android:layout_weight=""1""
                    android:layout_height=""wrap_content""
                    android:textColor=""?android:attr/textColorPrimary""
                    android:textAppearance=""@style/TextAppearance.AppCompat.Subhead""
                    android:lines=""1""
                    android:ellipsize=""end"" />
                <TextView
                    android:text=""6""
                    android:layout_width=""wrap_content""
                    android:layout_height=""wrap_content""
                    android:gravity=""right""
                    android:minWidth=""32dp""
                    android:id=""@+id/InfoViewSlotNumber""
                    android:textColor=""#000000""
                    android:textSize=""18sp""
                    android:textStyle=""bold"" />
                <ImageView
                    android:layout_width=""wrap_content""
                    android:layout_height=""wrap_content""
                    android:layout_marginLeft=""4dp""
                    android:src=""@android:color/transparent""
                    android:id=""@+id/InfoViewSlotNumberImg""
                    android:layout_gravity=""center_vertical"" />
                <TextView
                    android:id=""@+id/InfoViewDistance""
                    android:layout_width=""56dp""
                    android:layout_height=""wrap_content""
                    android:layout_marginLeft=""16dp""
                    android:lines=""1""
                    android:ellipsize=""end""
                    android:text=""6 miles""
                    android:gravity=""center_horizontal""
                    android:textColor=""?android:attr/textColorSecondary""
                    android:textAppearance=""@style/TextAppearance.AppCompat.Caption"" />
                <ImageView
                    android:id=""@+id/stationLock""
                    android:src=""@drawable/ic_station_lock""
                    android:layout_marginLeft=""35dp""
                    android:layout_marginRight=""19dp""
                    android:tint=""@color/lock_icon_tint""
                    android:scaleType=""center""
                    android:adjustViewBounds=""true""
                    android:layout_width=""wrap_content""
                    android:layout_height=""wrap_content""
                    android:visibility=""gone""
                    android:layout_gravity=""center_vertical"" />
            </LinearLayout>
        </LinearLayout>
    </FrameLayout>
    <LinearLayout
        android:background=""@color/card_background""
        android:orientation=""vertical""
        android:layout_width=""match_parent""
        android:layout_weight=""1""
        android:layout_height=""wrap_content"">
        <TextView
            android:text=""Activity""
            android:layout_width=""wrap_content""
            android:layout_height=""wrap_content""
            style=""@style/info_pane_section"" />
        <LinearLayout
            android:orientation=""vertical""
            android:layout_width=""match_parent""
            android:layout_height=""wrap_content"">
            <LinearLayout
                android:orientation=""horizontal""
                android:layout_width=""match_parent""
                android:layout_height=""wrap_content""
                android:background=""@drawable/time_cell_bg""
                android:paddingStart=""?android:attr/listPreferredItemPaddingStart""
                android:paddingEnd=""?android:attr/listPreferredItemPaddingEnd"">
                <ImageView
                    android:src=""@drawable/ic_clock""
                    android:layout_width=""wrap_content""
                    android:layout_height=""wrap_content""
                    android:id=""@+id/clockImg""
                    android:layout_gravity=""center_vertical""
                    android:layout_marginRight=""8dp""
                    android:tint=""@color/black_tint_primary""
                    android:adjustViewBounds=""true""
                    android:layout_marginTop=""3dp""
                    android:layout_marginBottom=""3dp"" />
                <TextView
                    android:id=""@+id/historyTime1""
                    style=""@style/activity_time_entry"" />
                <TextView
                    android:id=""@+id/historyTime2""
                    style=""@style/activity_time_entry"" />
                <TextView
                    android:id=""@+id/historyTime3""
                    style=""@style/activity_time_entry"" />
                <TextView
                    android:id=""@+id/historyTime4""
                    style=""@style/activity_time_entry"" />
                <TextView
                    android:id=""@+id/historyTime5""
                    style=""@style/activity_time_entry"" />
            </LinearLayout>
            <LinearLayout
                android:orientation=""horizontal""
                android:layout_width=""match_parent""
                android:layout_height=""wrap_content""
                android:paddingStart=""?android:attr/listPreferredItemPaddingStart""
                android:paddingEnd=""?android:attr/listPreferredItemPaddingEnd"">
                <ImageView
                    android:src=""@drawable/ic_bike_number""
                    android:layout_width=""wrap_content""
                    android:layout_height=""wrap_content""
                    android:id=""@+id/bikeNumberImg""
                    android:layout_gravity=""center_vertical""
                    android:layout_marginRight=""8dp""
                    android:adjustViewBounds=""true""
                    android:layout_marginTop=""3dp""
                    android:layout_marginBottom=""3dp""
                    android:tint=""@color/black_tint_primary"" />
                <TextView
                    android:id=""@+id/historyValue1""
                    style=""@style/activity_number_entry"" />
                <TextView
                    android:id=""@+id/historyValue2""
                    style=""@style/activity_number_entry"" />
                <TextView
                    android:id=""@+id/historyValue3""
                    style=""@style/activity_number_entry"" />
                <TextView
                    android:id=""@+id/historyValue4""
                    style=""@style/activity_number_entry"" />
                <TextView
                    android:id=""@+id/historyValue5""
                    style=""@style/activity_number_entry"" />
            </LinearLayout>
        </LinearLayout>
        <TextView
            android:text=""Location""
            android:layout_width=""wrap_content""
            android:layout_height=""wrap_content""
            style=""@style/info_pane_section"" />
        <com.google.android.gms.maps.StreetViewPanoramaView
            android:background=""@android:color/white""
            android:id=""@+id/streetViewPanorama""
            android:layout_width=""match_parent""
            android:layout_height=""160dp"" />
    </LinearLayout>
</moyeu.InfoPane>";
    }
}
