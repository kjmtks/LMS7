
start:
	docker-compose build
	docker-compose up

remove:
	docker-compose down
	docker-compose down
	docker volume rm alms_app-data alms_postgres-data

clean:
	rm -rf ALMS.App/bin
	rm -rf ALMS.App/obj
	rm -rf ALMS.App/etc
	rm -rf ALMS.App/node_modules
	rm -rf ALMS.App/out/wwwroot
