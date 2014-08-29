git add -A
git status
set /p var=Commit Note: 
git commit -m "%var%"
git push origin dev
pause