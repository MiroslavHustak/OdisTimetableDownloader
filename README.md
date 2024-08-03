# OdisTimetableDownloader

Hromadné stahování JŘ ODIS (varianta s immutable List)

Bulk downloading of timetables from the ODIS public transport system in Northern Moravia and (a part of) Silesia (these lands are located 
in the north-eastern part of the Czech Republic, in case you happen not to know it :-) ).

Stále ve vývojové fázi... :-). Varianta s Array (tento kód v tomto repozitáři není) je nevýznamně rychlejší (v průměru o cca 15-20 vteřin) než varianta s immutable List. Celková doba stahování je samozřejmě
závislá mj. na počtu vláken a rychlosti stahování, očekávejte řádově 3 až 10 minut.

Under development (a re-write of my old console app).

****************************************************************************************

Better error handling with more extensive utilization of result types and separation of the user interface from the business logic will be addressed 
during the process of converting this console application into a mobile application.

****************************************************************************************

Installation file (ClickOnce): https://1drv.ms/u/s!Aoxczq1nq-J5hx9vHNNAYDLLzr2b?e=u6hnEe
