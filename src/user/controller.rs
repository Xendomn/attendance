use crate::glob::Response;
use crate::user::service;
use crate::{user::bean::user::User, RB};
use actix_web::{get, post, web, HttpResponse, Scope};
use rbatis::crud::CRUD;

pub fn route_user() -> Scope {
    web::scope("user")
        .service(hello)
        .service(save)
        .service(get)
        .service(modify)
}

#[get("hello")]
pub async fn hello() -> HttpResponse {
    HttpResponse::Ok().body("hello world")
}

#[get("/{id}")]
pub async fn get(id: web::Path<i32>) -> HttpResponse {
    let res = RB.fetch_by_column("id", &id.into_inner()).await;
    match res {
        Ok(res) => {
            let res: User = res;
            let res = Response {
                code: Some(200),
                body: Some(res),
            };
            HttpResponse::Ok().json(res)
        }
        Err(err) => {
            println!("{}", err);
            HttpResponse::Ok().json(Response {
                code: Some(200),
                body: Some(err.to_string()),
            })
        }
    }
}

#[post("")]
pub async fn save(info: web::Json<User>) -> HttpResponse {
    let res = service::save(&info.into_inner()).await;

    if let Err(err) = res {
        eprintln!("Error saving service: {:?}", err);
    }

    HttpResponse::Ok().finish()
}

#[post("/{id}")]
pub async fn modify(
    id: web::Path<i32>,
    info: web::Json<User>,
) -> Result<HttpResponse, std::io::Error> {
    // search for user
    let info = info.into_inner();

    match &info.id {
        Some(info_id) => {
            if *info_id != id.into_inner() {
                eprintln!("id mismatch");
                return Err(std::io::Error::new(
                    std::io::ErrorKind::Interrupted,
                    "id mismatch",
                ));
            }
        }
        None => {
            eprintln!("no id in path");
            return Err(std::io::Error::new(
                std::io::ErrorKind::Interrupted,
                "no id in path",
            ));
        }
    }

    let res = RB.update_by_column("id", &info).await;

    if let Err(err) = res {
        eprintln!("Error updating columns: {:?}", err);
    };

    Ok(HttpResponse::Ok().finish())
}
