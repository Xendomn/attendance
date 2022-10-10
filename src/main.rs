use actix_web::middleware::Logger;
use actix_web::{App, HttpServer};
use env_logger::Env;
use tokio;

#[tokio::main]
async fn main() -> std::io::Result<()> {
    env_logger::init_from_env(Env::default().default_filter_or("info"));
    attendance::init().await;

    HttpServer::new(move || {
        // move counter into the closure
        App::new()
            .service(attendance::user::controller::route_user())
            .service(attendance::r#static::onedrive())
            .service(attendance::r#static::telegram())
            .wrap(Logger::new("%a %r %s %Dms"))
    })
    .workers(16)
    .bind(("127.0.0.1", 8000))?
    .run()
    .await
}
