IF NOT DEFINED Configuration SET Configuration=Release
MSBuild.exe EasyTabs.sln -t:clean
MSBuild.exe EasyTabs.sln -t:restore -p:RestorePackagesConfig=true
MSBuild.exe EasyTabs.sln -m /property:Configuration=%Configuration%
git add -A
git commit -a --allow-empty-message -m ''
git push